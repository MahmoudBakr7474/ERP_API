using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices;

// Posting an invoice, directly or by approval, as one transaction: the key is locked and looked up (a retry gets the
// first answer), then the invoice and its order are locked in that order. The order records what each line bills,
// which never exceeds what was received and not yet invoiced or returned, whatever other invoices did meanwhile; the
// invoice is numbered and becomes the recorded liability. Nothing touches inventory: a price variance stays on the invoice.
public sealed partial class InvoicePosting(
    ITransactionRunner transactions, IdempotentRequests requests, ISupplierInvoiceRepository invoices, IPurchaseOrderRepository orders,
    ISupplierInvoiceReadStore readStore, DocumentNumbering numbering, ICurrentUser currentUser, TimeProvider clock,
    ILogger<InvoicePosting> logger)
{
    public Task<Result<SupplierInvoicePostedResponse>> PostAsync(
        Guid invoiceId, byte[] rowVersion, Guid idempotencyKey, bool byApproval, CancellationToken ct) =>
        transactions.RunAsync(token => PostOnceAsync(invoiceId, rowVersion, idempotencyKey, byApproval, token), ct);

    private async Task<Result<SupplierInvoicePostedResponse>> PostOnceAsync(
        Guid invoiceId, byte[] rowVersion, Guid idempotencyKey, bool byApproval, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var operation = byApproval ? "ApproveSupplierInvoice" : "PostSupplierInvoice";
        var fingerprint = RequestFingerprint.Of(operation, invoiceId, rowVersion, userId);
        var previous = await requests.BeginAsync(idempotencyKey, fingerprint, ct);
        if (previous.IsFailure)
            return previous.Error;
        if (previous.Value is { } postedId)
            return await readStore.FindPostedAsync(postedId, ct)
                ?? throw new InvalidOperationException($"Idempotency record points at invoice {postedId}, which is not posted.");

        var invoice = await invoices.GetForUpdateAsync(invoiceId, expectedRowVersion: null, ct);
        if (invoice is null)
            return SupplierInvoiceErrors.NotFound;
        if (!invoice.RowVersion.AsSpan().SequenceEqual(rowVersion))
            return CommonErrors.ConcurrencyConflict;
        var allowed = invoice.CanPost(byApproval);
        if (allowed.IsFailure)
            return allowed.Error;
        var order = await orders.GetForUpdateAsync(invoice.PurchaseOrderId, expectedRowVersion: null, ct)
            ?? throw new InvalidOperationException($"Invoice {invoice.Id} points at order {invoice.PurchaseOrderId}, which does not exist.");
        var number = await numbering.NextAsync(PurchasingDocumentType.SupplierInvoice, invoice.BranchId, ct);
        if (number.IsFailure)
            return number.Error;

        var now = clock.GetUtcNow().UtcDateTime;
        var recorded = order.RecordInvoice(invoice.Matches(), number.Value.Number, userId, now);
        if (recorded.IsFailure)
            return recorded.Error;
        invoice.MarkPosted(number.Value, byApproval, userId, now);
        requests.Complete(idempotencyKey, operation, fingerprint, invoice.Id, userId, now);
        var saved = await invoices.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        InvoicePosted(logger, invoice.Id, number.Value.Number, invoice.PriceVarianceAmount);
        return new SupplierInvoicePostedResponse(invoice.Id, number.Value.Number, invoice.TotalAmount, invoice.PriceVarianceAmount,
            invoice.DueDate, now);
    }

    [LoggerMessage(EventId = 6030, Level = LogLevel.Information,
        Message = "Supplier invoice {InvoiceId} posted as {Number} with price variance {PriceVariance}")]
    private static partial void InvoicePosted(ILogger logger, Guid invoiceId, string number, decimal priceVariance);
}
