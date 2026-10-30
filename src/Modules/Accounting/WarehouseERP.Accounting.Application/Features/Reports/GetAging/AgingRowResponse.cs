using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetAging;

/// <summary>
/// One party's open balance by age, in the party's natural sign (what customers owe us, what we owe suppliers, positive).
/// Items on the other side (a customer's unallocated receipt or credit, a supplier's debit) are in <paramref name="Unallocated"/>,
/// negative.
/// </summary>
/// <param name="NotDue">Due on or after the date.</param>
public sealed record AgingRowResponse(
    PartyType PartyType, Guid PartyId, string? PartyCode, string? PartyNameAr, decimal NotDue, decimal Days1To30, decimal Days31To60,
    decimal Days61To90, decimal Over90Days, decimal Unallocated, decimal Total);
