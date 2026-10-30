namespace WarehouseERP.Inventory.Contracts;

/// <summary>When a posted stock document's cost entries were stamped, and the exact value they moved (in positive).</summary>
public sealed record InventoryDocumentPosting(Guid DocumentId, DateTime PostedAtUtc, decimal Value);
