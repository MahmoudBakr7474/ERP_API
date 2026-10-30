using WarehouseERP.Accounting.Application.Contracts.FiscalYears;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.FiscalYears;

/// <summary>Read side of fiscal years, their periods and their history.</summary>
public interface IFiscalYearReadStore
{
    /// <summary>Every year in date order.</summary>
    Task<IReadOnlyList<FiscalYearResponse>> ListAsync(CancellationToken ct);

    Task<FiscalYearResponse?> GetAsync(Guid fiscalYearId, CancellationToken ct);
}
