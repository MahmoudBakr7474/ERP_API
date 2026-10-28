namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.ApprovePurchaseRequest;

public sealed record ApprovePurchaseRequestCommand(Guid PurchaseRequestId, string RowVersion);
