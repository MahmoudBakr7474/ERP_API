using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetAging;

/// <summary>The aging of a party type with its column totals (the total equals the control accounts' open items).</summary>
public sealed record AgingResponse(
    PartyType PartyType, DateOnly AsOfDate, Guid? BranchId, string CurrencyCode, IReadOnlyList<AgingRowResponse> Rows, decimal NotDue,
    decimal Days1To30, decimal Days31To60, decimal Days61To90, decimal Over90Days, decimal Unallocated, decimal Total);
