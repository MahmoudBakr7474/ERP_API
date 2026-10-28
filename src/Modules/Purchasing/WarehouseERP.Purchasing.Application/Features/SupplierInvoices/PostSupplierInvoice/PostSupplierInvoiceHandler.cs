using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.PostSupplierInvoice;

// Direct posting of a draft without price differences, by someone who manages invoices in its warehouse.
public sealed class PostSupplierInvoiceHandler(ISupplierInvoiceRepository invoices, InvoicePosting posting, ICurrentUser currentUser)
    : ICommandHandler<PostSupplierInvoiceCommand, SupplierInvoicePostedResponse>
{
    public async Task<Result<SupplierInvoicePostedResponse>> HandleAsync(PostSupplierInvoiceCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await InvoiceAccess.AuthorizeAsync(invoices, currentUser, command.SupplierInvoiceId, ct);
        if (access.IsFailure)
            return access.Error;
        return await posting.PostAsync(command.SupplierInvoiceId, expected, command.IdempotencyKey, byApproval: false, ct);
    }
}
