using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Invoices;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Invoices.CreateSalesInvoice;

// A draft invoice of a confirmed order in the user's scope: each line bills an order line's delivered quantity that is not
// invoiced yet, at the order's agreed price, discount and VAT. Checked again, under the order's lock, when it is posted.
public sealed partial class CreateSalesInvoiceHandler(
    ISalesInvoiceRepository invoices, ISalesOrderRepository orders, SalesReferences references, ICurrentUser currentUser,
    TimeProvider clock, ILogger<CreateSalesInvoiceHandler> logger)
    : ICommandHandler<CreateSalesInvoiceCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateSalesInvoiceCommand command, CancellationToken ct)
    {
        var access = DocumentAccess.Check(currentUser, await orders.FindWarehouseAsync(command.OrderId, ct), OrderErrors.NotFound);
        if (access.IsFailure)
            return access.Error;
        var order = await orders.GetForUpdateAsync(command.OrderId, expectedRowVersion: null, ct);
        if (order is null)
            return OrderErrors.NotFound;
        var date = await InvoiceLines.DateAsync(references, command.InvoiceDate, ct);
        if (date.IsFailure)
            return date.Error;
        var lines = await InvoiceLines.ResolveAsync(references, order, command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;
        var created = SalesInvoice.CreateDraft(order, date.Value, command.CustomerReference, command.Notes, lines.Value,
            currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        invoices.Add(created.Value);
        var saved = await invoices.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        InvoiceCreated(logger, created.Value.Id, order.Id);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 7050, Level = LogLevel.Information, Message = "Sales invoice {InvoiceId} drafted for order {OrderId}")]
    private static partial void InvoiceCreated(ILogger logger, Guid invoiceId, Guid orderId);
}
