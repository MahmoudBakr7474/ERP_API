using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices;

// An invoice belongs to its order's warehouse scope. The endpoint already checked the permission of the action.
internal static class InvoiceAccess
{
    public static async Task<Result> AuthorizeAsync(ISupplierInvoiceRepository invoices, ICurrentUser user, Guid invoiceId, CancellationToken ct) =>
        DocumentAccess.Check(user, await invoices.FindWarehouseAsync(invoiceId, ct), SupplierInvoiceErrors.NotFound);
}
