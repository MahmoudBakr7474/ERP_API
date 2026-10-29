namespace WarehouseERP.Inventory.Contracts;

/// <summary>Base units a reservation operation reserved or released for one sales order line.</summary>
public sealed record ReservedQuantity(Guid SourceLineId, decimal BaseQuantity);
