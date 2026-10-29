using WarehouseERP.Sales.Application.Contracts.Documents;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Deliveries;

/// <param name="CostAmount">What the delivered goods cost at the moving average; 0 before posting.</param>
/// <param name="StockDocumentId">The SalesDelivery stock document Inventory posted.</param>
/// <param name="Documents">Customer returns from it, oldest first.</param>
/// <param name="RowVersion">Send it back with the next change or action.</param>
public sealed record SalesDeliveryDetailsResponse(
    Guid Id, string DraftReference, string? Number, SalesDeliveryStatus Status, Guid OrderId, string? OrderNumber, Guid CustomerId,
    string CustomerCode, string CustomerName, Guid WarehouseId, string WarehouseCode, Guid BranchId, string? ShippingAddress,
    string? Notes, decimal CostAmount, Guid? StockDocumentId, string? StockDocumentNumber, DateTime CreatedAtUtc, string? CreatedBy,
    DateTime? PostedAtUtc, string? PostedBy, string RowVersion, IReadOnlyList<SalesDeliveryLineResponse> Lines,
    IReadOnlyList<SalesLinkedDocumentResponse> Documents, IReadOnlyList<SalesDocumentEventResponse> Events);
