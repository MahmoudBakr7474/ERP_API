namespace WarehouseERP.Sales.Application.Features.Customers.SetCustomerActive;

public sealed record SetCustomerActiveCommand(Guid CustomerId, bool IsActive);
