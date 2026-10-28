using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.Transfers;

/// <param name="Reference">TR-000123, from creation on.</param>
/// <param name="Number">TRF-…, from dispatch on.</param>
/// <param name="OutstandingQuantity">Base quantity still in transit, over all lines.</param>
public sealed record StockTransferSummaryResponse(
    Guid Id, string Reference, string? Number, StockTransferStatus Status, Guid SourceWarehouseId, string SourceWarehouseCode,
    Guid DestinationWarehouseId, string DestinationWarehouseCode, Guid TransitWarehouseId, string TransitWarehouseCode,
    bool RequiresApproval, int LineCount, decimal OutstandingQuantity, DateTime CreatedAtUtc, Guid? CreatedByUserId,
    string? CreatedBy, DateTime? DispatchedAtUtc, DateTime? CompletedAtUtc);
