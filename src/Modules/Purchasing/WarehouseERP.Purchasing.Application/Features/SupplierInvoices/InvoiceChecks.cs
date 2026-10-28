using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices;

// What a draft invoice is checked against besides its order: an invoice date that is not in the future (in the company's
// time zone) and a supplier invoice number no other invoice of the supplier has. The database's filtered unique index
// still decides when two saves race.
internal static class InvoiceChecks
{
    public static async Task<Result> CheckAsync(
        PurchasingReferences references, ISupplierInvoiceRepository invoices, SupplierInvoice invoice, CancellationToken ct)
    {
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        if (invoice.InvoiceDate > references.Today(company.Value))
            return SupplierInvoiceErrors.InvoiceDateInFuture;
        if (await invoices.InvoiceNumberTakenAsync(invoice.SupplierId, invoice.SupplierInvoiceNumberKey, invoice.Id, ct))
            return SupplierInvoiceErrors.InvoiceNumberTaken;
        return Result.Success();
    }
}
