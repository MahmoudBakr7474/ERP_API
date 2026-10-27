using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Companies;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Infrastructure.Persistence;

internal sealed class CompanyRepository(DbContext db) : ICompanyRepository
{
    // With one company per installation no unique index can clash on an expected path.
    private static readonly Dictionary<string, Error> NoKnownConstraints = new(StringComparer.Ordinal);

    public async Task<Result> AddIfNoneAsync(Company company, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // UPDLOCK + HOLDLOCK lock the table's range, empty or not, until commit: a second setup waits here and then
        // sees the first one's company.
        var existing = await db.Database
            .SqlQuery<int>($"SELECT TOP (1) 1 AS [Value] FROM [organization].[Companies] WITH (UPDLOCK, HOLDLOCK)")
            .ToListAsync(ct);
        if (existing.Count > 0)
            return CompanyErrors.AlreadyConfigured;

        db.Set<Company>().Add(company);
        var saved = await DbFailures.SaveAsync(db, NoKnownConstraints, ct);
        if (saved.IsSuccess)
            await transaction.CommitAsync(ct);
        return saved;
    }

    public async Task<Company?> GetForUpdateAsync(byte[]? expectedRowVersion, CancellationToken ct)
    {
        var company = await db.Set<Company>().SingleOrDefaultAsync(ct);
        if (company is not null && expectedRowVersion is not null)
            // The UPDATE's WHERE uses the original value, so this makes it compare with the caller's version.
            db.Entry(company).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return company;
    }

    public Task<Result> SaveChangesAsync(CancellationToken ct) => DbFailures.SaveAsync(db, NoKnownConstraints, ct);
}
