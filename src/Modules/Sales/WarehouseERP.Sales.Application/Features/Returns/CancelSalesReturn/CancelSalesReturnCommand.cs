namespace WarehouseERP.Sales.Application.Features.Returns.CancelSalesReturn;

public sealed record CancelSalesReturnCommand(Guid ReturnId, string RowVersion);
