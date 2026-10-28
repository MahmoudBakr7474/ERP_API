namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.RejectSupplierReturn;

/// <param name="Reason">Why it goes back to draft; kept in its history.</param>
public sealed record RejectSupplierReturnCommand(Guid SupplierReturnId, string Reason, string RowVersion);
