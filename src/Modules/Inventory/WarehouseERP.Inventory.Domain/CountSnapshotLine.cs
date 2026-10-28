namespace WarehouseERP.Inventory.Domain;

/// <summary>A balance in the count's scope when it was frozen: what the count expects to find.</summary>
public sealed record CountSnapshotLine(Guid ProductId, Guid LocationId, decimal Quantity);
