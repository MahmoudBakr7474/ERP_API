using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.RejectSupplierInvoice;

// An approver sends a submitted invoice back to draft with a reason. Returns the new row version.
public sealed partial class RejectSupplierInvoiceHandler(
    ISupplierInvoiceRepository invoices, ICurrentUser currentUser, TimeProvider clock, ILogger<RejectSupplierInvoiceHandler> logger)
    : ICommandHandler<RejectSupplierInvoiceCommand, string>
{
    public async Task<Result<string>> HandleAsync(RejectSupplierInvoiceCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await InvoiceAccess.AuthorizeAsync(invoices, currentUser, command.SupplierInvoiceId, ct);
        if (access.IsFailure)
            return access.Error;
        var invoice = await invoices.GetForUpdateAsync(command.SupplierInvoiceId, expected, ct);
        if (invoice is null)
            return SupplierInvoiceErrors.NotFound;

        var rejected = invoice.Reject(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (rejected.IsFailure)
            return rejected.Error;
        var saved = await invoices.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        InvoiceRejected(logger, invoice.Id);
        return Convert.ToBase64String(invoice.RowVersion);
    }

    [LoggerMessage(EventId = 6034, Level = LogLevel.Information, Message = "Supplier invoice {InvoiceId} rejected")]
    private static partial void InvoiceRejected(ILogger logger, Guid invoiceId);
}
