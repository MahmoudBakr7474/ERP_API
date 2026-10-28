namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// An authorized decrease (an adjustment or a reversal) left a warehouse with less sellable stock than is reserved. The
/// posting stands; this records the condition for someone to resolve, it never changes a reservation by itself.
/// </summary>
public sealed class OverReservationAlert
{
    private OverReservationAlert() { } // EF Core

    public long Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid DocumentId { get; private set; }
    public decimal OnHandSellable { get; private set; }
    public decimal ReservedQuantity { get; private set; }
    public DateTime DetectedAtUtc { get; private set; }

    public static OverReservationAlert Record(
        Guid productId, Guid warehouseId, Guid documentId, decimal onHandSellable, decimal reservedQuantity, DateTime detectedAtUtc) =>
        new()
        {
            ProductId = productId, WarehouseId = warehouseId, DocumentId = documentId, OnHandSellable = onHandSellable,
            ReservedQuantity = reservedQuantity, DetectedAtUtc = detectedAtUtc,
        };
}
