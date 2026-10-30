namespace WarehouseERP.Inventory.Contracts;

/// <summary>
/// Read-only view of posted stock value for Accounting (ADR 0020, Milestone C): which documents changed the company's stock
/// value, by how much, and the valuation at a moment. Accounting journals them later and never changes anything here, so a
/// posting never waits on Accounting.
/// </summary>
public interface IInventoryAccountingSource
{
    /// <summary>
    /// Posted documents whose value only Inventory explains (opening stock, receipts and issues without a business document,
    /// adjustments, their reversals and count adjustments), posted at or after <paramref name="fromUtc"/> and before
    /// <paramref name="toUtc"/>, oldest first. Transfers move no value, and what Purchasing, Sales or POS posted is theirs to
    /// report, so neither is listed.
    /// </summary>
    Task<IReadOnlyList<InventoryPostedDocument>> ListPostedAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct);

    /// <summary>A posted document's value (any type), or null when it does not exist or is not posted.</summary>
    Task<InventoryDocumentValue?> GetDocumentAsync(Guid documentId, CancellationToken ct);

    /// <summary>
    /// The exact value each posted document moved: the sum of its cost entries' value deltas (positive in, negative out).
    /// Documents with no cost entry are left out.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetDocumentValuesAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken ct);

    /// <summary>When each posted document's cost entries were stamped and what they moved; documents with none are left out.</summary>
    Task<IReadOnlyList<InventoryDocumentPosting>> GetPostingsAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken ct);

    /// <summary>The company's stock value just before the moment: the sum of every cost entry posted before it.</summary>
    Task<decimal> ValuationBeforeAsync(DateTime beforeUtc, CancellationToken ct);
}
