using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;

public interface ISupplierInvoiceReadStore
{
    Task<SupplierInvoiceDetailsResponse?> GetAsync(Guid invoiceId, CancellationToken ct);

    Task<PagedResult<SupplierInvoiceSummaryResponse>> ListAsync(SupplierInvoiceFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>What posting answered, for a retry with the same Idempotency-Key.</summary>
    Task<SupplierInvoicePostedResponse?> FindPostedAsync(Guid invoiceId, CancellationToken ct);
}
