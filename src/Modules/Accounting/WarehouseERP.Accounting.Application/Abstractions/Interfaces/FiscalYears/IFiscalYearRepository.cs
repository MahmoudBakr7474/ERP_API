using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.FiscalYears;

/// <summary>Write side of fiscal years and their periods; implemented over the shared ErpDbContext.</summary>
public interface IFiscalYearRepository
{
    Task<bool> AnyAsync(CancellationToken ct);

    /// <summary>The year that ends last, untracked, or null.</summary>
    Task<FiscalYear?> LatestAsync(CancellationToken ct);

    Task<bool> OverlapsAsync(DateOnly start, DateOnly end, CancellationToken ct);

    /// <summary>Tracked with its periods, or null; with a row version, the save fails with a conflict unless it still has it.</summary>
    Task<FiscalYear?> GetForUpdateAsync(Guid fiscalYearId, byte[]? expectedRowVersion, CancellationToken ct);

    /// <summary>The year a period belongs to, or null when there is no such period.</summary>
    Task<Guid?> FindYearIdOfPeriodAsync(Guid periodId, CancellationToken ct);

    /// <summary>The period whose dates hold this date, read now (untracked), or null.</summary>
    Task<PeriodRef?> FindPeriodAsync(DateOnly date, CancellationToken ct);

    /// <summary>The period's state read now, or null.</summary>
    Task<FiscalPeriodStatus?> PeriodStatusAsync(Guid periodId, CancellationToken ct);

    /// <summary>True when a year starting before this date is still open.</summary>
    Task<bool> AnyOpenBeforeAsync(DateOnly start, CancellationToken ct);

    /// <summary>True when a year starting after this date is closed.</summary>
    Task<bool> AnyClosedAfterAsync(DateOnly end, CancellationToken ct);

    void Add(FiscalYear fiscalYear);

    /// <summary>The save point; a year created twice at the same moment comes back as a conflict.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
