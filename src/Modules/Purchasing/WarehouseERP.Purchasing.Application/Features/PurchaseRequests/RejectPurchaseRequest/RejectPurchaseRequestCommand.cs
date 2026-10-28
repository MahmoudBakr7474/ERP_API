namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.RejectPurchaseRequest;

/// <param name="Reason">Why it goes back to draft; kept in its history.</param>
public sealed record RejectPurchaseRequestCommand(Guid PurchaseRequestId, string Reason, string RowVersion);
