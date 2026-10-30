using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.CashAccounts.ListCashAccounts;

/// <summary>Cashboxes and bank accounts with their balances: the cash and bank balances report.</summary>
/// <param name="AsOfDate">Balances at the end of this date; today's postings included when null.</param>
public sealed record ListCashAccountsQuery(CashAccountKind? Kind, Guid? BranchId, bool? IsActive, DateOnly? AsOfDate);
