namespace WarehouseERP.Accounting.Application.Features.SourcePostings.GetSourceReconciliation;

/// <summary>
/// Source-to-ledger reconciliation (V1 decision 6): stock value, the clearing accounts and the documents themselves.
/// </summary>
/// <param name="ClearingAccounts">Goods received not invoiced, unbilled receivables and the tills' cash: the ledger's balance against what the modules hold now; they agree once every document is journaled.</param>
/// <param name="MissingCount">Documents posted since the cutover that have no accounting identity yet (only the first 200 are listed).</param>
/// <param name="UnmatchedJournals">Journals and accounting identities that do not point at each other (should be none; duplicates are impossible, the database refuses them).</param>
/// <param name="OpenCardIncidents">Cards charged at a till with no sale recorded, still to reverse or settle.</param>
/// <param name="CashDifferences">Shifts closed since the cutover whose counted cash differed from the expected cash.</param>
/// <param name="IsReconciled">Nothing unexplained, missing, unposted or unmatched.</param>
public sealed record SourceReconciliationResponse(
    bool IsActivated, DateOnly CutoverDate, DateTime AsOfUtc, InventoryReconciliationResponse Inventory,
    IReadOnlyList<ClearingAccountResponse> ClearingAccounts, IReadOnlyList<SourceStatusCountResponse> Counts, int MissingCount,
    IReadOnlyList<MissingSourceResponse> Missing, int UnmatchedJournals, IReadOnlyList<OpenCardIncidentResponse> OpenCardIncidents,
    IReadOnlyList<CashDifferenceResponse> CashDifferences, bool IsReconciled);
