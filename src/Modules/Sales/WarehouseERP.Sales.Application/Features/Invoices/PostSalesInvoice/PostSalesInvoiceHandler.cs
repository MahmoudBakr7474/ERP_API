using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Invoices;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Contracts.Invoices;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Invoices.PostSalesInvoice;

// Posting is one transaction: the request's key is locked and looked up (a retry gets the first answer), the invoice is
// locked at the version the user saw, then its order, so two invoices of one order can't bill the same delivered goods:
// each line is checked again against what was delivered and not invoiced or returned before invoicing (a database check
// backs it). The invoice is numbered and the order records it. Posted invoices never change. No payment, receivable or
// ledger entry is made in V1.
public sealed partial class PostSalesInvoiceHandler(
    ITransactionRunner transactions, IdempotentRequests requests, ISalesInvoiceRepository invoices, ISalesOrderRepository orders,
    ISalesInvoiceReadStore readStore, SalesReferences references, DocumentNumbering numbering, ICurrentUser currentUser,
    TimeProvider clock, ILogger<PostSalesInvoiceHandler> logger)
    : ICommandHandler<PostSalesInvoiceCommand, SalesInvoicePostedResponse>
{
    private const string Operation = "PostSalesInvoice";

    public async Task<Result<SalesInvoicePostedResponse>> HandleAsync(PostSalesInvoiceCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await InvoiceAccess.AuthorizeAsync(invoices, currentUser, command.InvoiceId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => PostAsync(command, expected, token), ct);
    }

    private async Task<Result<SalesInvoicePostedResponse>> PostAsync(PostSalesInvoiceCommand command, byte[] expected, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var fingerprint = RequestFingerprint.Of(Operation, command.InvoiceId, expected, userId);
        var previous = await requests.BeginAsync(command.IdempotencyKey, fingerprint, ct);
        if (previous.IsFailure)
            return previous.Error;
        if (previous.Value is { } postedId)
            return await readStore.FindPostedAsync(postedId, ct)
                ?? throw new InvalidOperationException($"Idempotency record points at invoice {postedId}, which is not posted.");

        var invoice = await invoices.GetForUpdateAsync(command.InvoiceId, expected, ct);
        if (invoice is null)
            return InvoiceErrors.NotFound;
        if (invoice.Status != SalesInvoiceStatus.Draft)
            return SalesInvoiceDomainErrors.NotDraft;
        var order = await orders.GetForUpdateAsync(invoice.OrderId, expectedRowVersion: null, ct)
            ?? throw new InvalidOperationException($"Invoice {invoice.Id} points at order {invoice.OrderId}, which does not exist.");
        var date = await InvoiceLines.DateAsync(references, invoice.InvoiceDate, ct);
        if (date.IsFailure)
            return date.Error;
        var billable = order.CanInvoice(invoice.Matches().Select(m => new OrderLineQuantity(m.OrderLineId, m.Quantity)).ToList());
        if (billable.IsFailure)
            return billable.Error;
        var number = await numbering.NextAsync(SalesDocumentType.Invoice, invoice.BranchId, ct);
        if (number.IsFailure)
            return number.Error;

        var now = clock.GetUtcNow().UtcDateTime;
        var posted = invoice.MarkPosted(number.Value, userId, now);
        if (posted.IsFailure)
            return posted.Error;
        var recorded = order.RecordInvoice(invoice.Matches(), invoice.TotalAmount, number.Value.Number, userId, now);
        if (recorded.IsFailure)
            return recorded.Error;
        requests.Complete(command.IdempotencyKey, Operation, fingerprint, invoice.Id, userId, now);

        var saved = await invoices.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        InvoicePosted(logger, invoice.Id, number.Value.Number, order.Id);
        return new SalesInvoicePostedResponse(invoice.Id, number.Value.Number, invoice.TotalAmount, invoice.DueDate, now);
    }

    [LoggerMessage(EventId = 7053, Level = LogLevel.Information, Message = "Sales invoice {InvoiceId} posted as {Number} for order {OrderId}")]
    private static partial void InvoicePosted(ILogger logger, Guid invoiceId, string number, Guid orderId);
}
