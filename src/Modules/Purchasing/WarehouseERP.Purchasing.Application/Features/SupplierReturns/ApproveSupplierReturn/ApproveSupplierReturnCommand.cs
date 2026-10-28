namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.ApproveSupplierReturn;

/// <summary>Approves a submitted return, which posts it: the goods leave stock.</summary>
/// <param name="IdempotencyKey">A new GUID per approval click, sent again unchanged when the client retries.</param>
public sealed record ApproveSupplierReturnCommand(Guid SupplierReturnId, string RowVersion, Guid IdempotencyKey);
