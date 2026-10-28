namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.CancelPurchaseRequest;

public sealed record CancelPurchaseRequestCommand(Guid PurchaseRequestId, string RowVersion);
