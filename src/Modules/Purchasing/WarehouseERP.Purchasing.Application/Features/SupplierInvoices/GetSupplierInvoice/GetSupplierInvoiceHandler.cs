using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.GetSupplierInvoice;

public sealed class GetSupplierInvoiceHandler(ISupplierInvoiceRepository invoices, ISupplierInvoiceReadStore readStore, ICurrentUser currentUser)
    : IQueryHandler<GetSupplierInvoiceQuery, SupplierInvoiceDetailsResponse>
{
    public async Task<Result<SupplierInvoiceDetailsResponse>> HandleAsync(GetSupplierInvoiceQuery query, CancellationToken ct)
    {
        var access = await InvoiceAccess.AuthorizeAsync(invoices, currentUser, query.SupplierInvoiceId, ct);
        if (access.IsFailure)
            return access.Error;
        return await readStore.GetAsync(query.SupplierInvoiceId, ct) is { } invoice ? invoice : SupplierInvoiceErrors.NotFound;
    }
}
