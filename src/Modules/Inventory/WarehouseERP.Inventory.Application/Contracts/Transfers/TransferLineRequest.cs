namespace WarehouseERP.Inventory.Application.Contracts.Transfers;

/// <summary>One line of a transfer as the client sends it.</summary>
/// <param name="ProductUnitId">The product unit the quantity is in: any active unit of the product.</param>
/// <param name="Quantity">Quantity in that unit, up to 4 decimals (whole for a unit that cannot be split).</param>
/// <param name="SourceLocationId">Where it is picked from in the source warehouse; the DEFAULT location when left out.</param>
public sealed record TransferLineRequest(Guid ProductId, Guid ProductUnitId, decimal Quantity, Guid? SourceLocationId = null);
