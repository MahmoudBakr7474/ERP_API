using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.SubmitSupplierInvoice;

// Only an invoice with price differences is submitted; one without posts directly. Returns the new row version.
public sealed partial class SubmitSupplierInvoiceHandler(
    ISupplierInvoiceRepository invoices, ICurrentUser currentUser, TimeProvider clock, ILogger<SubmitSupplierInvoiceHandler> logger)
    : ICommandHandler<SubmitSupplierInvoiceCommand, string>
{
    public async Task<Result<string>> HandleAsync(SubmitSupplierInvoiceCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await InvoiceAccess.AuthorizeAsync(invoices, currentUser, command.SupplierInvoiceId, ct);
        if (access.IsFailure)
            return access.Error;
        var invoice = await invoices.GetForUpdateAsync(command.SupplierInvoiceId, expected, ct);
        if (invoice is null)
            return SupplierInvoiceErrors.NotFound;

        var submitted = invoice.Submit(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (submitted.IsFailure)
            return submitted.Error;
        var saved = await invoices.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        InvoiceSubmitted(logger, invoice.Id);
        return Convert.ToBase64String(invoice.RowVersion);
    }

    [LoggerMessage(EventId = 6033, Level = LogLevel.Information, Message = "Supplier invoice {InvoiceId} submitted for approval")]
    private static partial void InvoiceSubmitted(ILogger logger, Guid invoiceId);
}
