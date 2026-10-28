namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.SubmitSupplierReturn;

public sealed record SubmitSupplierReturnCommand(Guid SupplierReturnId, string RowVersion);
