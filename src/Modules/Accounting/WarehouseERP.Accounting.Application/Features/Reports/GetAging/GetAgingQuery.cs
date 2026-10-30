using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetAging;

/// <summary>Customers' or suppliers' open balances at the end of a date, by how long they have been due.</summary>
/// <param name="BranchId">Only items of this branch.</param>
public sealed record GetAgingQuery(PartyType PartyType, DateOnly AsOfDate, Guid? BranchId);
