namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.CancelSupplierReturn;

public sealed record CancelSupplierReturnCommand(Guid SupplierReturnId, string RowVersion);
