using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Features.ProductTaxes.ListProductTaxes;

public sealed record ListProductTaxesQuery(int Page, int? PageSize, Guid? ProductId, SalesTaxTreatment? TaxTreatment) : IPagedQuery;
