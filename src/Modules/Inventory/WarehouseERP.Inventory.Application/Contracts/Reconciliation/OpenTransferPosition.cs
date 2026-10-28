using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.Reconciliation;

/// <param name="OutstandingQuantity">Base quantity still in transit over all lines.</param>
/// <param name="DaysInTransit">Whole days since dispatch.</param>
public sealed record OpenTransferPosition(
    Guid TransferId, string? Number, StockTransferStatus Status, string SourceWarehouseCode, string DestinationWarehouseCode,
    DateTime? DispatchedAtUtc, int DaysInTransit, decimal OutstandingQuantity);
