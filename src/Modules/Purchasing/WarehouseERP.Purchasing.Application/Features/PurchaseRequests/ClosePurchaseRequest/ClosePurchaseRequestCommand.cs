namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.ClosePurchaseRequest;

/// <param name="Reason">Why the rest will not be ordered; kept on the request.</param>
public sealed record ClosePurchaseRequestCommand(Guid PurchaseRequestId, string Reason, string RowVersion);
