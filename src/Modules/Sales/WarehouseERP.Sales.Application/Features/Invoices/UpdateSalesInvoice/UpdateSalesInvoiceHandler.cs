using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Invoices;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Invoices.UpdateSalesInvoice;

// Only a draft, only from the version the editor read; the lines are checked again against what may be billed. Returns
// the new row version.
public sealed partial class UpdateSalesInvoiceHandler(
    ISalesInvoiceRepository invoices, ISalesOrderRepository orders, SalesReferences references, ICurrentUser currentUser,
    ILogger<UpdateSalesInvoiceHandler> logger)
    : ICommandHandler<UpdateSalesInvoiceCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateSalesInvoiceCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await InvoiceAccess.AuthorizeAsync(invoices, currentUser, command.InvoiceId, ct);
        if (access.IsFailure)
            return access.Error;
        var invoice = await invoices.GetForUpdateAsync(command.InvoiceId, expected, ct);
        if (invoice is null)
            return InvoiceErrors.NotFound;
        var order = await orders.GetForUpdateAsync(invoice.OrderId, expectedRowVersion: null, ct)
            ?? throw new InvalidOperationException($"Invoice {invoice.Id} points at order {invoice.OrderId}, which does not exist.");
        var date = await InvoiceLines.DateAsync(references, command.InvoiceDate, ct);
        if (date.IsFailure)
            return date.Error;
        var lines = await InvoiceLines.ResolveAsync(references, order, command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;
        var replaced = invoice.ReplaceDraft(order, date.Value, command.CustomerReference, command.Notes, lines.Value);
        if (replaced.IsFailure)
            return replaced.Error;
        var saved = await invoices.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        InvoiceUpdated(logger, invoice.Id);
        return Convert.ToBase64String(invoice.RowVersion);
    }

    [LoggerMessage(EventId = 7051, Level = LogLevel.Information, Message = "Sales invoice {InvoiceId} updated")]
    private static partial void InvoiceUpdated(ILogger logger, Guid invoiceId);
}
