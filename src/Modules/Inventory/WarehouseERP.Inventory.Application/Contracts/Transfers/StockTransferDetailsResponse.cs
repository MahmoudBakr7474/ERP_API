using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.Transfers;

/// <param name="TransitLocationCode">The location opened for this transfer in the transit warehouse, from dispatch on.</param>
/// <param name="CreatedByUserId">Its creator.</param>
/// <param name="RowVersion">Send it back with the next change or action.</param>
/// <param name="Documents">The dispatch, receipts and loss adjustment it produced.</param>
public sealed record StockTransferDetailsResponse(
    Guid Id, string Reference, string? Number, StockTransferStatus Status, Guid SourceWarehouseId, string SourceWarehouseCode,
    Guid DestinationWarehouseId, string DestinationWarehouseCode, Guid TransitWarehouseId, string TransitWarehouseCode,
    Guid? TransitLocationId, string? TransitLocationCode, bool RequiresApproval, string? ExternalReference, string? Notes,
    DateTime CreatedAtUtc, Guid? CreatedByUserId, string? CreatedBy, DateTime? DispatchedAtUtc, string? DispatchedBy,
    string? ApprovedBy, DateTime? CompletedAtUtc, decimal OutstandingQuantity, string RowVersion,
    IReadOnlyList<StockTransferLineResponse> Lines, IReadOnlyList<StockTransferDocumentResponse> Documents,
    IReadOnlyList<StockTransferEventResponse> Events);
