using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetSubledgerReconciliation;

/// <summary>One control account from both sides (debit minus credit).</summary>
/// <param name="OpenItemsAmount">What its open items amount to (each posted line opens one).</param>
/// <param name="OpenItemsOpenAmount">What is still open of them after allocations (allocations move amounts between items, never out).</param>
/// <param name="LinesWithoutParty">Posted lines on it naming no party (never expected).</param>
public sealed record SubledgerAccountResponse(
    Guid AccountId, string AccountCode, string AccountNameAr, PartyType PartyType, decimal LedgerBalance, decimal OpenItemsAmount,
    decimal OpenItemsOpenAmount, decimal LinesWithoutParty, decimal Difference, bool IsReconciled);
