using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Features.Quotations.ListSalesQuotations;

/// <param name="Search">Matches the number, the draft reference or the customer's reference.</param>
public sealed record ListSalesQuotationsQuery(
    int Page, int? PageSize, Guid? WarehouseId, Guid? CustomerId, SalesQuotationStatus? Status, string? Search) : IPagedQuery;
