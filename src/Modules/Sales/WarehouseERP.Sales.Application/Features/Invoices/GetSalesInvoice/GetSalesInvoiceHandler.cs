using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Invoices;
using WarehouseERP.Sales.Application.Contracts.Invoices;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Invoices.GetSalesInvoice;

public sealed class GetSalesInvoiceHandler(ISalesInvoiceRepository invoices, ISalesInvoiceReadStore readStore, ICurrentUser currentUser)
    : IQueryHandler<GetSalesInvoiceQuery, SalesInvoiceDetailsResponse>
{
    public async Task<Result<SalesInvoiceDetailsResponse>> HandleAsync(GetSalesInvoiceQuery query, CancellationToken ct)
    {
        var access = await InvoiceAccess.AuthorizeAsync(invoices, currentUser, query.InvoiceId, ct);
        if (access.IsFailure)
            return access.Error;
        return await readStore.GetAsync(query.InvoiceId, ct) is { } invoice ? invoice : InvoiceErrors.NotFound;
    }
}
