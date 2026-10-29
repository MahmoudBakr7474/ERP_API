namespace WarehouseERP.Sales.Application.Features.Returns.RejectSalesReturn;

/// <param name="Reason">Why it goes back to draft; kept in its history.</param>
public sealed record RejectSalesReturnCommand(Guid ReturnId, string Reason, string RowVersion);
