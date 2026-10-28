using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.Valuation;

/// <param name="UnitCost">Cost per base unit this step used.</param>
public sealed record InventoryCostEntryResponse(
    long Id, DateTime PostedAtUtc, Guid DocumentId, string DocumentNumber, StockDocumentType DocumentType,
    decimal QuantityDelta, decimal ValueDelta, decimal UnitCost, decimal QuantityAfter, decimal ValueAfter,
    decimal AverageCostAfter);
