using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.Stock;

/// <summary>One line of a stock card.</summary>
/// <param name="Quantity">Base quantity: positive in, negative out.</param>
/// <param name="BalanceAfter">The product's balance after this movement, in the warehouse (or the location, when filtered by one).</param>
public sealed record StockMovementResponse(
    long Id, DateTime PostedAtUtc, Guid DocumentId, string DocumentNumber, StockDocumentType DocumentType,
    Guid LocationId, string LocationCode, decimal Quantity, decimal BalanceAfter);
