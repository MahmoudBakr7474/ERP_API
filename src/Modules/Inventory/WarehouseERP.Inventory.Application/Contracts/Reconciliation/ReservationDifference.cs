namespace WarehouseERP.Inventory.Application.Contracts.Reconciliation;

/// <param name="ReservedQuantity">The warehouse's ProductWarehouseStock.ReservedQuantity.</param>
/// <param name="HeldByReservations">What the product's reservations in the warehouse still hold.</param>
public sealed record ReservationDifference(
    Guid ProductId, string Sku, Guid WarehouseId, string WarehouseCode, decimal ReservedQuantity, decimal HeldByReservations);
