using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.CreateSupplierInvoice;

// A draft invoice of the order's supplier, matched to the order's lines: each bills at most what was received and is not
// yet invoiced (checked again when it posts). A net price unlike the order's marks it as needing approval. One save point.
public sealed partial class CreateSupplierInvoiceHandler(
    ISupplierInvoiceRepository invoices, IPurchaseOrderRepository orders, PurchasingReferences references, ICurrentUser currentUser,
    TimeProvider clock, ILogger<CreateSupplierInvoiceHandler> logger)
    : ICommandHandler<CreateSupplierInvoiceCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateSupplierInvoiceCommand command, CancellationToken ct)
    {
        var access = DocumentAccess.Check(currentUser, await orders.FindWarehouseAsync(command.PurchaseOrderId, ct), PurchaseOrderErrors.NotFound);
        if (access.IsFailure)
            return access.Error;
        var order = await orders.GetForUpdateAsync(command.PurchaseOrderId, expectedRowVersion: null, ct);
        if (order is null)
            return PurchaseOrderErrors.NotFound;

        var created = SupplierInvoice.CreateDraft(order,
            new InvoiceTerms(command.SupplierInvoiceNumber, command.InvoiceDate, command.DueDate, command.Notes),
            InvoiceLines.Inputs(command.Lines), currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        var checkedInvoice = await InvoiceChecks.CheckAsync(references, invoices, created.Value, ct);
        if (checkedInvoice.IsFailure)
            return checkedInvoice.Error;

        invoices.Add(created.Value);
        var saved = await invoices.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        InvoiceCreated(logger, created.Value.Id, order.Id, created.Value.RequiresApproval);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 6031, Level = LogLevel.Information,
        Message = "Supplier invoice {InvoiceId} drafted for order {OrderId}; needs approval: {RequiresApproval}")]
    private static partial void InvoiceCreated(ILogger logger, Guid invoiceId, Guid orderId, bool requiresApproval);
}
