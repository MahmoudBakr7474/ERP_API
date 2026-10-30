namespace WarehouseERP.Accounting.Application.Contracts.Cutover;

/// <summary>One check of the cutover.</summary>
/// <param name="Code">Stable: CutoverStarted, FiscalPeriodOpen, PostingRolesMapped, OpeningBalancesPosted, SubledgersReconciled, InventoryValuation, GoodsReceivedNotInvoiced, UnbilledReceivables, PosTillCash.</param>
/// <param name="Expected">For a comparison, what the operational modules hold at the cutover.</param>
/// <param name="Actual">For a comparison, what the ledger's opening balances hold.</param>
/// <param name="Message">What to do when it fails.</param>
public sealed record CutoverCheckResponse(string Code, bool Passed, decimal? Expected, decimal? Actual, string? Message);
