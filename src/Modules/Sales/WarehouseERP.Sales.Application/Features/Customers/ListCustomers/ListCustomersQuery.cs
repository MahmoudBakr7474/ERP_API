using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Features.Customers.ListCustomers;

/// <param name="Search">Matches the code, either name, the mobile or phone, or the tax registration number.</param>
public sealed record ListCustomersQuery(
    int Page, int? PageSize, string? Search, bool? IsActive, CustomerType? CustomerType, bool? IsOnCreditHold, string? CountryCode)
    : IPagedQuery;
