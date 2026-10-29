using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Quotations;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Quotations.SubmitSalesQuotation;

// The sales settings decide now whether the draft needs an approver (policy, discount threshold, manual or below-list
// prices); otherwise it is issued and numbered at once, in one transaction with its number. Its customer must still be
// active. Returns the new row version.
public sealed partial class SubmitSalesQuotationHandler(
    ITransactionRunner transactions, ISalesQuotationRepository quotations, ISalesSettingsRepository settings, SalesReferences references,
    DocumentNumbering numbering, ICurrentUser currentUser, TimeProvider clock, ILogger<SubmitSalesQuotationHandler> logger)
    : ICommandHandler<SubmitSalesQuotationCommand, string>
{
    public async Task<Result<string>> HandleAsync(SubmitSalesQuotationCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await QuotationAccess.AuthorizeAsync(quotations, currentUser, command.QuotationId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => SubmitAsync(command.QuotationId, expected, token), ct);
    }

    private async Task<Result<string>> SubmitAsync(Guid quotationId, byte[] expected, CancellationToken ct)
    {
        var quotation = await quotations.GetForUpdateAsync(quotationId, expected, ct);
        if (quotation is null)
            return QuotationErrors.NotFound;
        if (quotation.Status != SalesQuotationStatus.Draft)
            return SalesQuotationDomainErrors.NotDraft;
        var customer = await references.ActiveCustomerAsync(quotation.CustomerId, ct);
        if (customer.IsFailure)
            return customer.Error;

        var now = clock.GetUtcNow().UtcDateTime;
        var reasons = (await settings.GetAsync(ct)).ApprovalReasons(quotation.Lines, isOrder: false, customerOnCreditHold: false);
        Result done;
        if (reasons.Count > 0)
            done = quotation.Submit(reasons, currentUser.UserId, now);
        else
        {
            var number = await numbering.NextAsync(SalesDocumentType.Quotation, quotation.BranchId, ct);
            if (number.IsFailure)
                return number.Error;
            done = quotation.IssueDirectly(number.Value, currentUser.UserId, now);
        }
        if (done.IsFailure)
            return done.Error;
        var saved = await quotations.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        QuotationSubmitted(logger, quotation.Id, quotation.Status);
        return Convert.ToBase64String(quotation.RowVersion);
    }

    [LoggerMessage(EventId = 7022, Level = LogLevel.Information, Message = "Sales quotation {QuotationId} submitted: now {Status}")]
    private static partial void QuotationSubmitted(ILogger logger, Guid quotationId, SalesQuotationStatus status);
}
