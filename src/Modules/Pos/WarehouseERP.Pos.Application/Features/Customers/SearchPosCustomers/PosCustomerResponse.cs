using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Pos.Application.Features.Customers.SearchPosCustomers;

/// <summary>A registered customer a cashier may name on a sale.</summary>
public sealed record PosCustomerResponse(
    Guid Id, string Code, string NameAr, string? NameEn, CustomerType CustomerType, string? TaxRegistrationNumber, string? Mobile);
