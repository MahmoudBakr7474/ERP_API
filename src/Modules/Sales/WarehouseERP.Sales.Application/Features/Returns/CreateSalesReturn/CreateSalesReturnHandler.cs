using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Deliveries;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Returns.CreateSalesReturn;

// A draft return of a posted delivery in the user's scope: each line at most what its delivery line has not had back yet,
// fit for resale, into a location of the delivery's warehouse. Checked again, under the order's and delivery's locks, when
// it is approved.
public sealed partial class CreateSalesReturnHandler(
    ISalesReturnRepository returns, ISalesDeliveryRepository deliveries, SalesReferences references, ICurrentUser currentUser,
    TimeProvider clock, ILogger<CreateSalesReturnHandler> logger)
    : ICommandHandler<CreateSalesReturnCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateSalesReturnCommand command, CancellationToken ct)
    {
        var access = DocumentAccess.Check(currentUser, await deliveries.FindWarehouseAsync(command.DeliveryId, ct), DeliveryErrors.NotFound);
        if (access.IsFailure)
            return access.Error;
        var delivery = await deliveries.GetForUpdateAsync(command.DeliveryId, expectedRowVersion: null, ct);
        if (delivery is null)
            return DeliveryErrors.NotFound;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var lines = await ReturnLines.ResolveAsync(references, delivery, command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;
        var created = SalesReturn.CreateDraft(delivery, company.Value.BaseCurrencyCode, command.Reason, command.Notes, lines.Value,
            currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        returns.Add(created.Value);
        var saved = await returns.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        ReturnCreated(logger, created.Value.Id, delivery.Id);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 7060, Level = LogLevel.Information, Message = "Customer return {ReturnId} drafted for delivery {DeliveryId}")]
    private static partial void ReturnCreated(ILogger logger, Guid returnId, Guid deliveryId);
}
