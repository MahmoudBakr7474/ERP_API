using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.ApproveSupplierInvoice;

// An approver (endpoint permission, warehouse scope), whoever entered the invoice included, accepts its price differences:
// approving posts it, and the variances stay recorded on its lines.
public sealed class ApproveSupplierInvoiceHandler(ISupplierInvoiceRepository invoices, InvoicePosting posting, ICurrentUser currentUser)
    : ICommandHandler<ApproveSupplierInvoiceCommand, SupplierInvoicePostedResponse>
{
    public async Task<Result<SupplierInvoicePostedResponse>> HandleAsync(ApproveSupplierInvoiceCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await InvoiceAccess.AuthorizeAsync(invoices, currentUser, command.SupplierInvoiceId, ct);
        if (access.IsFailure)
            return access.Error;
        return await posting.PostAsync(command.SupplierInvoiceId, expected, command.IdempotencyKey, byApproval: true, ct);
    }
}
