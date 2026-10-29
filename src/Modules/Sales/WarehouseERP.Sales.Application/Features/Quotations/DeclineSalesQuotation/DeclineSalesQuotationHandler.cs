using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Quotations;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Quotations.DeclineSalesQuotation;

// The customer said no to an issued quotation. Returns the new row version.
public sealed partial class DeclineSalesQuotationHandler(
    ISalesQuotationRepository quotations, ICurrentUser currentUser, TimeProvider clock, ILogger<DeclineSalesQuotationHandler> logger)
    : ICommandHandler<DeclineSalesQuotationCommand, string>
{
    public async Task<Result<string>> HandleAsync(DeclineSalesQuotationCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await QuotationAccess.AuthorizeAsync(quotations, currentUser, command.QuotationId, ct);
        if (access.IsFailure)
            return access.Error;
        var quotation = await quotations.GetForUpdateAsync(command.QuotationId, expected, ct);
        if (quotation is null)
            return QuotationErrors.NotFound;
        var declined = quotation.Decline(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (declined.IsFailure)
            return declined.Error;
        var saved = await quotations.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        QuotationDeclined(logger, quotation.Id);
        return Convert.ToBase64String(quotation.RowVersion);
    }

    [LoggerMessage(EventId = 7025, Level = LogLevel.Information, Message = "Sales quotation {QuotationId} declined by the customer")]
    private static partial void QuotationDeclined(ILogger logger, Guid quotationId);
}
