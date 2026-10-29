using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Quotations;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Quotations.ApproveSalesQuotation;

// An approver (endpoint permission, warehouse scope), its creator included (CLAUDE.md), approves a submitted quotation
// of a customer that is still active: it is issued and numbered in the same transaction. Returns the new row version.
public sealed partial class ApproveSalesQuotationHandler(
    ITransactionRunner transactions, ISalesQuotationRepository quotations, SalesReferences references, DocumentNumbering numbering,
    ICurrentUser currentUser, TimeProvider clock, ILogger<ApproveSalesQuotationHandler> logger)
    : ICommandHandler<ApproveSalesQuotationCommand, string>
{
    public async Task<Result<string>> HandleAsync(ApproveSalesQuotationCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await QuotationAccess.AuthorizeAsync(quotations, currentUser, command.QuotationId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => ApproveAsync(command.QuotationId, expected, token), ct);
    }

    private async Task<Result<string>> ApproveAsync(Guid quotationId, byte[] expected, CancellationToken ct)
    {
        var quotation = await quotations.GetForUpdateAsync(quotationId, expected, ct);
        if (quotation is null)
            return QuotationErrors.NotFound;
        var allowed = quotation.CanApprove();
        if (allowed.IsFailure)
            return allowed.Error;
        var customer = await references.ActiveCustomerAsync(quotation.CustomerId, ct);
        if (customer.IsFailure)
            return customer.Error;
        var number = await numbering.NextAsync(SalesDocumentType.Quotation, quotation.BranchId, ct);
        if (number.IsFailure)
            return number.Error;
        var approved = quotation.Approve(number.Value, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (approved.IsFailure)
            return approved.Error;
        var saved = await quotations.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        QuotationApproved(logger, quotation.Id, number.Value.Number);
        return Convert.ToBase64String(quotation.RowVersion);
    }

    [LoggerMessage(EventId = 7023, Level = LogLevel.Information, Message = "Sales quotation {QuotationId} approved and issued as {Number}")]
    private static partial void QuotationApproved(ILogger logger, Guid quotationId, string number);
}
