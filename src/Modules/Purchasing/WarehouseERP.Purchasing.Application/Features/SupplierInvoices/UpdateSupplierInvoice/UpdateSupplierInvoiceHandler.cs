using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.UpdateSupplierInvoice;

// Only a draft changes, only from the version its editor read (409 otherwise). Returns the new row version.
public sealed partial class UpdateSupplierInvoiceHandler(
    ISupplierInvoiceRepository invoices, IPurchaseOrderRepository orders, PurchasingReferences references, ICurrentUser currentUser,
    ILogger<UpdateSupplierInvoiceHandler> logger)
    : ICommandHandler<UpdateSupplierInvoiceCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateSupplierInvoiceCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await InvoiceAccess.AuthorizeAsync(invoices, currentUser, command.SupplierInvoiceId, ct);
        if (access.IsFailure)
            return access.Error;
        var invoice = await invoices.GetForUpdateAsync(command.SupplierInvoiceId, expected, ct);
        if (invoice is null)
            return SupplierInvoiceErrors.NotFound;
        var order = await orders.GetForUpdateAsync(invoice.PurchaseOrderId, expectedRowVersion: null, ct)
            ?? throw new InvalidOperationException($"Invoice {invoice.Id} points at order {invoice.PurchaseOrderId}, which does not exist.");

        var replaced = invoice.ReplaceDraft(order,
            new InvoiceTerms(command.SupplierInvoiceNumber, command.InvoiceDate, command.DueDate, command.Notes),
            InvoiceLines.Inputs(command.Lines));
        if (replaced.IsFailure)
            return replaced.Error;
        var checkedInvoice = await InvoiceChecks.CheckAsync(references, invoices, invoice, ct);
        if (checkedInvoice.IsFailure)
            return checkedInvoice.Error;
        var saved = await invoices.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        InvoiceUpdated(logger, invoice.Id);
        return Convert.ToBase64String(invoice.RowVersion);
    }

    [LoggerMessage(EventId = 6032, Level = LogLevel.Information, Message = "Supplier invoice {InvoiceId} draft updated")]
    private static partial void InvoiceUpdated(ILogger logger, Guid invoiceId);
}
