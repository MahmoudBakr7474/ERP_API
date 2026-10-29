namespace WarehouseERP.Sales.Application.Features.Returns.SubmitSalesReturn;

public sealed record SubmitSalesReturnCommand(Guid ReturnId, string RowVersion);
