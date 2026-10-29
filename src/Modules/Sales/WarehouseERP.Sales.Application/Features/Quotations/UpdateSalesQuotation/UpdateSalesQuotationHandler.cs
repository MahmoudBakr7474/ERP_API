using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Quotations;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Quotations.UpdateSalesQuotation;

// Only a draft, only from the version the editor read; its lines are priced again as sent. Returns the new row version.
public sealed partial class UpdateSalesQuotationHandler(
    ISalesQuotationRepository quotations, SalesReferences references, LinePricing pricing, ICurrentUser currentUser,
    ILogger<UpdateSalesQuotationHandler> logger)
    : ICommandHandler<UpdateSalesQuotationCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateSalesQuotationCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await QuotationAccess.AuthorizeAsync(quotations, currentUser, command.QuotationId, ct);
        if (access.IsFailure)
            return access.Error;
        var quotation = await quotations.GetForUpdateAsync(command.QuotationId, expected, ct);
        if (quotation is null)
            return QuotationErrors.NotFound;
        var customer = await references.ActiveCustomerAsync(command.CustomerId, ct);
        if (customer.IsFailure)
            return customer.Error;
        var lines = await pricing.PriceAsync(command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;
        var replaced = quotation.ReplaceDraft(new QuotationTerms(command.CustomerId, command.QuotationDate, command.ValidUntil,
            command.CustomerReference, command.PaymentTermDays, command.Notes), lines.Value);
        if (replaced.IsFailure)
            return replaced.Error;
        var saved = await quotations.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        QuotationUpdated(logger, quotation.Id);
        return Convert.ToBase64String(quotation.RowVersion);
    }

    [LoggerMessage(EventId = 7021, Level = LogLevel.Information, Message = "Sales quotation {QuotationId} updated")]
    private static partial void QuotationUpdated(ILogger logger, Guid quotationId);
}
