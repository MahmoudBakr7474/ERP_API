namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.SubmitPurchaseRequest;

/// <summary>Submits a draft for approval, or approves and numbers it at once when its policy needs no approval.</summary>
public sealed record SubmitPurchaseRequestCommand(Guid PurchaseRequestId, string RowVersion);
