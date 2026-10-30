using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.FiscalYears;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Infrastructure.Persistence;

internal sealed class FiscalYearRepository(DbContext db) : IFiscalYearRepository
{
    public Task<bool> AnyAsync(CancellationToken ct) => db.Set<FiscalYear>().AnyAsync(ct);

    public Task<FiscalYear?> LatestAsync(CancellationToken ct) =>
        db.Set<FiscalYear>().AsNoTracking().OrderByDescending(x => x.EndDate).FirstOrDefaultAsync(ct);

    public Task<bool> OverlapsAsync(DateOnly start, DateOnly end, CancellationToken ct) =>
        db.Set<FiscalYear>().AnyAsync(x => x.StartDate <= end && x.EndDate >= start, ct);

    public async Task<FiscalYear?> GetForUpdateAsync(Guid fiscalYearId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var year = await db.Set<FiscalYear>().Include(x => x.Periods).SingleOrDefaultAsync(x => x.Id == fiscalYearId, ct);
        if (year is not null && expectedRowVersion is not null)
            db.Entry(year).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return year;
    }

    public async Task<Guid?> FindYearIdOfPeriodAsync(Guid periodId, CancellationToken ct) =>
        await db.Set<FiscalPeriod>().Where(x => x.Id == periodId).Select(x => (Guid?)x.FiscalYearId).SingleOrDefaultAsync(ct);

    public Task<PeriodRef?> FindPeriodAsync(DateOnly date, CancellationToken ct) =>
        (from period in db.Set<FiscalPeriod>()
         join year in db.Set<FiscalYear>() on period.FiscalYearId equals year.Id
         where period.StartDate <= date && period.EndDate >= date
         select new PeriodRef(year.Id, year.StartDate, period.Id, period.Status)).SingleOrDefaultAsync(ct);

    public async Task<FiscalPeriodStatus?> PeriodStatusAsync(Guid periodId, CancellationToken ct) =>
        await db.Set<FiscalPeriod>().Where(x => x.Id == periodId).Select(x => (FiscalPeriodStatus?)x.Status).SingleOrDefaultAsync(ct);

    public Task<bool> AnyOpenBeforeAsync(DateOnly start, CancellationToken ct) =>
        db.Set<FiscalYear>().AnyAsync(x => x.StartDate < start && x.Status == FiscalYearStatus.Open, ct);

    public Task<bool> AnyClosedAfterAsync(DateOnly end, CancellationToken ct) =>
        db.Set<FiscalYear>().AnyAsync(x => x.StartDate > end && x.Status == FiscalYearStatus.Closed, ct);

    public void Add(FiscalYear fiscalYear) => db.Add(fiscalYear);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => AccountingSaves.SaveAsync(db, ct);
}
