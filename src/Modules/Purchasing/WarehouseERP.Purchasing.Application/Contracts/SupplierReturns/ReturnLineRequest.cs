namespace WarehouseERP.Purchasing.Application.Contracts.SupplierReturns;

/// <summary>One line of a supplier return as the client sends it.</summary>
/// <param name="GoodsReceiptLineId">A line of the return's goods receipt; each at most once.</param>
/// <param name="Quantity">In the order line's unit; at most what the receipt brought in and was not returned yet.</param>
/// <param name="LocationId">Where the goods leave from; the location they were received into when left out.</param>
public sealed record ReturnLineRequest(Guid GoodsReceiptLineId, decimal Quantity, Guid? LocationId = null);
