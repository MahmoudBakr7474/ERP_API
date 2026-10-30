namespace WarehouseERP.Accounting.Application.Features.SourcePostings.GetSourceReconciliation;

/// <summary>
/// The inventory account against Inventory's valuation (its cost entries), with the difference explained.
/// </summary>
/// <param name="Difference">Ledger balance less valuation.</param>
/// <param name="OpeningDifference">The opening balance less the exact valuation at the cutover (the rounding to the minor unit when they agree).</param>
/// <param name="RoundingDifference">What journals posted for stock less what the cost entries moved exactly (each document rounded once).</param>
/// <param name="UnpostedValue">Stock value moved by documents not journaled yet.</param>
/// <param name="Unexplained">Difference less the opening's rounding and the journals' rounding plus the unposted value: zero when they reconcile.</param>
public sealed record InventoryReconciliationResponse(
    decimal LedgerBalance, decimal Valuation, decimal Difference, decimal OpeningDifference, decimal RoundingDifference,
    decimal UnpostedValue, decimal Unexplained);
