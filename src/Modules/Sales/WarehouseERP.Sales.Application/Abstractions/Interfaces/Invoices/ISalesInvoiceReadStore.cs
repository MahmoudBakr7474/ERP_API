using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Contracts.Invoices;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Invoices;

public interface ISalesInvoiceReadStore
{
    Task<SalesInvoiceDetailsResponse?> GetAsync(Guid invoiceId, CancellationToken ct);

    Task<PagedResult<SalesInvoiceSummaryResponse>> ListAsync(SalesInvoiceFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>What posting answered, for a retry with the same idempotency key; null unless it is posted.</summary>
    Task<SalesInvoicePostedResponse?> FindPostedAsync(Guid invoiceId, CancellationToken ct);
}
