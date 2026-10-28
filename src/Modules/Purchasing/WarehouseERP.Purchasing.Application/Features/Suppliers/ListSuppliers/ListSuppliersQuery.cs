using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Purchasing.Application.Features.Suppliers.ListSuppliers;

/// <param name="Search">Matches the code, either name or the tax registration number.</param>
public sealed record ListSuppliersQuery(int Page, int? PageSize, string? Search, bool? IsActive, string? CountryCode) : IPagedQuery;
