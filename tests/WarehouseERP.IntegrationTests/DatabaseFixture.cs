using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Catalog;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.Identity.Contracts;
using WarehouseERP.Inventory;
using WarehouseERP.Organization;
using WarehouseERP.Organization.Domain;
using WarehouseERP.Persistence;

namespace WarehouseERP.IntegrationTests;

// One throwaway SQL Server database per test run: migrations applied once, dropped at the end.
// Tests share it, so every test creates its own rows with unique codes.
public sealed class DatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; } =
        $"Server={Environment.GetEnvironmentVariable("ERP_TEST_SQL_SERVER") ?? @"(localdb)\MSSQLLocalDB"};" +
        $"Database=WarehouseERP_Tests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 1, 15, 8, 30, 0, TimeSpan.Zero));

    /// <summary>A context stamped by the auditing interceptor, as in the app, acting as <paramref name="user"/>.</summary>
    public ErpDbContext NewDb(ICurrentUser? user = null) =>
        new(new DbContextOptionsBuilder<ErpDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(new AuditingSaveChangesInterceptor(Clock, user ?? TestCurrentUser.AllAccess()))
            .Options);

    /// <summary>The modules wired the way the host wires them, minus HTTP.</summary>
    public ServiceProvider Services(ICurrentUser? user = null) =>
        new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Database:ConnectionString"] = ConnectionString })
                .Build())
            .AddLogging()
            .AddSingleton<TimeProvider>(Clock)
            .AddSingleton(user ?? TestCurrentUser.AllAccess())
            .Configure<PaginationOptions>(_ => { })
            .AddErpPersistence()
            .AddOrganizationModule()
            .AddCatalogModule()
            .AddInventoryModule()
            .AddScoped<IUserDirectory, NoUserNames>() // Identity's directory; module tests don't need names
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

    public async Task InitializeAsync()
    {
        await using var db = NewDb();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = NewDb();
        await db.Database.EnsureDeletedAsync();
    }

    public static string Code(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..20];

    private readonly SemaphoreSlim _companyLock = new(1, 1);
    private Guid? _companyId;

    /// <summary>The database's one company (V1 has one per installation), created on first use.</summary>
    public async Task<Guid> EnsureCompanyAsync()
    {
        await _companyLock.WaitAsync();
        try
        {
            if (_companyId is { } id)
                return id;
            await using var db = NewDb();
            var company = Company.Create(Code("C"), "الشركة", null, "EGP", "Africa/Cairo", null);
            db.Add(company);
            await db.SaveChangesAsync();
            _companyId = company.Id;
            return company.Id;
        }
        finally
        {
            _companyLock.Release();
        }
    }

    public async Task<(Guid BranchId, Guid UnitId)> SeedBranchAndUnitAsync()
    {
        var companyId = await EnsureCompanyAsync();
        await using var db = NewDb();
        var company = await db.Set<Company>().SingleAsync(x => x.Id == companyId);
        var branch = company.AddBranch(Code("B"), "فرع", null);
        var unit = Unit.Create(Code("U"), "قطعة", "Piece", allowsFraction: false);
        db.AddRange(branch, unit);
        await db.SaveChangesAsync();
        return (branch.Id, unit.Id);
    }
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}

internal static class DbAssert
{
    // Runs SaveChanges and expects SQL Server to refuse it through the named constraint or index.
    public static async Task RejectedBy(DbContext db, string constraintName)
    {
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Contains(constraintName, ex.InnerException?.Message);
    }

    // Runs raw SQL (bypassing the domain on purpose) and expects the named constraint to refuse it.
    public static async Task SqlRejectedBy(DbContext db, FormattableString sql, string constraintName)
    {
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlInterpolatedAsync(sql));
        Assert.Contains(constraintName, ex.Message);
    }
}
