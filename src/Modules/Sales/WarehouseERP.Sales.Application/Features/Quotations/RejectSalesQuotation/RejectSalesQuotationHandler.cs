using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Quotations;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Quotations.RejectSalesQuotation;

// An approver sends a submitted quotation back to draft with a reason. Returns the new row version.
public sealed partial class RejectSalesQuotationHandler(
    ISalesQuotationRepository quotations, ICurrentUser currentUser, TimeProvider clock, ILogger<RejectSalesQuotationHandler> logger)
    : ICommandHandler<RejectSalesQuotationCommand, string>
{
    public async Task<Result<string>> HandleAsync(RejectSalesQuotationCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await QuotationAccess.AuthorizeAsync(quotations, currentUser, command.QuotationId, ct);
        if (access.IsFailure)
            return access.Error;
        var quotation = await quotations.GetForUpdateAsync(command.QuotationId, expected, ct);
        if (quotation is null)
            return QuotationErrors.NotFound;
        var rejected = quotation.Reject(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (rejected.IsFailure)
            return rejected.Error;
        var saved = await quotations.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        QuotationRejected(logger, quotation.Id);
        return Convert.ToBase64String(quotation.RowVersion);
    }

    [LoggerMessage(EventId = 7024, Level = LogLevel.Information, Message = "Sales quotation {QuotationId} rejected")]
    private static partial void QuotationRejected(ILogger logger, Guid quotationId);
}
