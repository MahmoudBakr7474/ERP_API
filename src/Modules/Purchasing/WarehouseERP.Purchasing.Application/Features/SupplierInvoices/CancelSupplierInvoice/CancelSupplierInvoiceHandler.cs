using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.CancelSupplierInvoice;

// A posted invoice is immutable; only a draft or submitted one is abandoned. Returns the new row version.
public sealed partial class CancelSupplierInvoiceHandler(
    ISupplierInvoiceRepository invoices, ICurrentUser currentUser, TimeProvider clock, ILogger<CancelSupplierInvoiceHandler> logger)
    : ICommandHandler<CancelSupplierInvoiceCommand, string>
{
    public async Task<Result<string>> HandleAsync(CancelSupplierInvoiceCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await InvoiceAccess.AuthorizeAsync(invoices, currentUser, command.SupplierInvoiceId, ct);
        if (access.IsFailure)
            return access.Error;
        var invoice = await invoices.GetForUpdateAsync(command.SupplierInvoiceId, expected, ct);
        if (invoice is null)
            return SupplierInvoiceErrors.NotFound;

        var cancelled = invoice.Cancel(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (cancelled.IsFailure)
            return cancelled.Error;
        var saved = await invoices.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        InvoiceCancelled(logger, invoice.Id);
        return Convert.ToBase64String(invoice.RowVersion);
    }

    [LoggerMessage(EventId = 6035, Level = LogLevel.Information, Message = "Supplier invoice {InvoiceId} cancelled")]
    private static partial void InvoiceCancelled(ILogger logger, Guid invoiceId);
}
