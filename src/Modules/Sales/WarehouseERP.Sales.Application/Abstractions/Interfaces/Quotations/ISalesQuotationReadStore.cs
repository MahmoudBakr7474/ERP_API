using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Contracts.Quotations;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Quotations;

public interface ISalesQuotationReadStore
{
    /// <param name="today">The business date, which decides whether an issued quotation shows as expired.</param>
    Task<SalesQuotationDetailsResponse?> GetAsync(Guid quotationId, DateOnly today, CancellationToken ct);

    Task<PagedResult<SalesQuotationSummaryResponse>> ListAsync(
        SalesQuotationFilter filter, DateOnly today, int page, int pageSize, CancellationToken ct);
}
