using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.FiscalYears;
using WarehouseERP.Accounting.Application.Contracts.FiscalYears;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.Identity.Contracts;

namespace WarehouseERP.Accounting.Infrastructure.ReadModels;

internal sealed class FiscalYearReadStore(DbContext db, IUserDirectory users) : IFiscalYearReadStore
{
    public async Task<IReadOnlyList<FiscalYearResponse>> ListAsync(CancellationToken ct) =>
        await MapAsync(await Query().OrderBy(x => x.StartDate).ToListAsync(ct), ct);

    public async Task<FiscalYearResponse?> GetAsync(Guid fiscalYearId, CancellationToken ct) =>
        (await MapAsync(await Query().Where(x => x.Id == fiscalYearId).ToListAsync(ct), ct)).SingleOrDefault();

    private IQueryable<FiscalYear> Query() =>
        db.Set<FiscalYear>().AsNoTracking().Include(x => x.Periods).Include(x => x.Events).AsSplitQuery();

    private async Task<IReadOnlyList<FiscalYearResponse>> MapAsync(List<FiscalYear> years, CancellationToken ct)
    {
        var closingIds = years.Where(y => y.ClosingJournalId is not null).Select(y => y.ClosingJournalId!.Value).ToArray();
        var numbers = closingIds.Length == 0
            ? new Dictionary<Guid, string?>()
            : await db.Set<JournalEntry>().AsNoTracking().Where(x => closingIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Number, ct);
        var userIds = years.SelectMany(y => y.Events).Where(e => e.UserId is not null).Select(e => e.UserId!.Value).Distinct().ToList();
        IReadOnlyDictionary<Guid, string> names = userIds.Count == 0 ? new Dictionary<Guid, string>() : await users.GetDisplayNamesAsync(userIds, ct);
        return years.Select(y => new FiscalYearResponse(
            y.Id, y.Name, y.StartDate, y.EndDate, y.Status, y.ClosedAtUtc, y.ClosedByUserId, y.ClosingJournalId,
            y.ClosingJournalId is { } closingId ? numbers.GetValueOrDefault(closingId) : null,
            y.Periods.OrderBy(p => p.Number)
                .Select(p => new FiscalPeriodResponse(p.Id, p.Number, p.StartDate, p.EndDate, p.Status, p.ClosedAtUtc, p.ClosedByUserId))
                .ToList(),
            y.Events.OrderBy(e => e.Id)
                .Select(e => new FiscalYearEventResponse(e.Action, e.PeriodNumber, e.UserId,
                    e.UserId is { } userId ? names.GetValueOrDefault(userId) : null, e.AtUtc, e.Note))
                .ToList(),
            Convert.ToBase64String(y.RowVersion))).ToList();
    }
}
