using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Invoices;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Invoices;

// An invoice belongs to its warehouse's scope (its order's). The endpoint already checked the permission of the action.
internal static class InvoiceAccess
{
    public static async Task<Result> AuthorizeAsync(ISalesInvoiceRepository invoices, ICurrentUser user, Guid invoiceId, CancellationToken ct) =>
        DocumentAccess.Check(user, await invoices.FindWarehouseAsync(invoiceId, ct), InvoiceErrors.NotFound);
}
