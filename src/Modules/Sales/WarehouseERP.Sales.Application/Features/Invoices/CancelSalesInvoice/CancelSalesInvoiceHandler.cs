using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Invoices;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Invoices.CancelSalesInvoice;

// Only a draft invoice is abandoned; a posted one never changes (a customer return records the credit instead). Returns
// the new row version.
public sealed partial class CancelSalesInvoiceHandler(
    ISalesInvoiceRepository invoices, ICurrentUser currentUser, TimeProvider clock, ILogger<CancelSalesInvoiceHandler> logger)
    : ICommandHandler<CancelSalesInvoiceCommand, string>
{
    public async Task<Result<string>> HandleAsync(CancelSalesInvoiceCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await InvoiceAccess.AuthorizeAsync(invoices, currentUser, command.InvoiceId, ct);
        if (access.IsFailure)
            return access.Error;
        var invoice = await invoices.GetForUpdateAsync(command.InvoiceId, expected, ct);
        if (invoice is null)
            return InvoiceErrors.NotFound;
        var cancelled = invoice.Cancel(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (cancelled.IsFailure)
            return cancelled.Error;
        var saved = await invoices.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        InvoiceCancelled(logger, invoice.Id);
        return Convert.ToBase64String(invoice.RowVersion);
    }

    [LoggerMessage(EventId = 7052, Level = LogLevel.Information, Message = "Sales invoice {InvoiceId} cancelled")]
    private static partial void InvoiceCancelled(ILogger logger, Guid invoiceId);
}
