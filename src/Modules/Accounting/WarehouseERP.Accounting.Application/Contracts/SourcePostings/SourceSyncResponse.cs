namespace WarehouseERP.Accounting.Application.Contracts.SourcePostings;

/// <summary>What one run did.</summary>
/// <param name="Discovered">Documents found for the first time.</param>
/// <param name="Posted">Journals posted.</param>
/// <param name="NothingToPost">Documents that moved no amount the ledger records.</param>
/// <param name="Failed">Documents whose journal could not post (they stay in the queue and are tried again).</param>
/// <param name="MoreDue">True when the run stopped at its batch size with more documents due.</param>
public sealed record SourceSyncResponse(int Discovered, int Posted, int NothingToPost, int Failed, bool MoreDue);
