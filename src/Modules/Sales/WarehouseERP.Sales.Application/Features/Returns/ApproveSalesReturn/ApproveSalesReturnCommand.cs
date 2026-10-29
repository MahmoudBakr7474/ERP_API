namespace WarehouseERP.Sales.Application.Features.Returns.ApproveSalesReturn;

/// <param name="RowVersion">The version of the submitted return the approver saw.</param>
/// <param name="IdempotencyKey">From the Idempotency-Key header: a new GUID per click, sent again unchanged on a retry.</param>
public sealed record ApproveSalesReturnCommand(Guid ReturnId, string RowVersion, Guid IdempotencyKey);
