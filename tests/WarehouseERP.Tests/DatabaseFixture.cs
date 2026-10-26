using Microsoft.EntityFrameworkCore;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.Organization.Domain;
using WarehouseERP.Persistence;

namespace WarehouseERP.Tests;

// One throwaway database per test run: migrations applied once, dropped at the end.
// Tests share it, so every test creates its own rows with unique codes.
public sealed class DatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; } =
        $"Server={Environment.GetEnvironmentVariable("ERP_TEST_SQL_SERVER") ?? @"(localdb)\MSSQLLocalDB"};" +
        $"Database=WarehouseERP_Tests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";

    public ErpDbContext NewDb() =>
        new(new DbContextOptionsBuilder<ErpDbContext>().UseSqlServer(ConnectionString).Options);

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

    public async Task<(Guid BranchId, Guid UnitId)> SeedBranchAndUnitAsync()
    {
        await using var db = NewDb();
        var company = new Company { Code = Code("C"), NameAr = "الشركة", BaseCurrencyCode = "EGP", TimeZoneId = "Africa/Cairo" };
        var branch = new Branch { Company = company, Code = Code("B"), NameAr = "فرع" };
        var unit = new Unit { Code = Code("U"), NameAr = "قطعة" };
        db.AddRange(company, branch, unit);
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
    public static async Task RejectedBy(ErpDbContext db, string constraintName)
    {
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Contains(constraintName, ex.InnerException?.Message);
    }
}
