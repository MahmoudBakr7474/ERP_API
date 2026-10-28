namespace WarehouseERP.Inventory.Domain;

/// <summary>Part of a transfer line arriving, in base units, and where it goes in the destination warehouse.</summary>
/// <param name="BaseProductUnitId">The product's base product unit, which the receipt document line uses.</param>
public sealed record TransferReceiptInput(
    Guid LineId, decimal Quantity, Guid LocationId, Guid BaseProductUnitId, Guid BaseUnitId, bool BaseUnitAllowsFraction);
