using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Quotations;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Quotations.CancelSalesQuotation;

// A quotation the customer has not accepted is abandoned; it never reserved anything. Returns the new row version.
public sealed partial class CancelSalesQuotationHandler(
    ISalesQuotationRepository quotations, ICurrentUser currentUser, TimeProvider clock, ILogger<CancelSalesQuotationHandler> logger)
    : ICommandHandler<CancelSalesQuotationCommand, string>
{
    public async Task<Result<string>> HandleAsync(CancelSalesQuotationCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await QuotationAccess.AuthorizeAsync(quotations, currentUser, command.QuotationId, ct);
        if (access.IsFailure)
            return access.Error;
        var quotation = await quotations.GetForUpdateAsync(command.QuotationId, expected, ct);
        if (quotation is null)
            return QuotationErrors.NotFound;
        var cancelled = quotation.Cancel(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (cancelled.IsFailure)
            return cancelled.Error;
        var saved = await quotations.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        QuotationCancelled(logger, quotation.Id);
        return Convert.ToBase64String(quotation.RowVersion);
    }

    [LoggerMessage(EventId = 7026, Level = LogLevel.Information, Message = "Sales quotation {QuotationId} cancelled")]
    private static partial void QuotationCancelled(ILogger logger, Guid quotationId);
}
