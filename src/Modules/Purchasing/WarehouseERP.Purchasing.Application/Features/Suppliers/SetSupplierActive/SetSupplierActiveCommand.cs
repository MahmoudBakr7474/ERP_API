namespace WarehouseERP.Purchasing.Application.Features.Suppliers.SetSupplierActive;

public sealed record SetSupplierActiveCommand(Guid SupplierId, bool IsActive);
