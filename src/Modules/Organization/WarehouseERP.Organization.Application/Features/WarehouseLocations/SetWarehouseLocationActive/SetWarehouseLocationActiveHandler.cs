using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.WarehouseLocations.SetWarehouseLocationActive;

// The domain keeps the default location active.
public sealed partial class SetWarehouseLocationActiveHandler(
    IWarehouseRepository warehouses, ICurrentUser currentUser, ILogger<SetWarehouseLocationActiveHandler> logger)
    : ICommandHandler<SetWarehouseLocationActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetWarehouseLocationActiveCommand command, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(command.WarehouseId))
            return WarehouseErrors.AccessDenied;
        var location = await warehouses.GetLocationForUpdateAsync(command.WarehouseId, command.LocationId, expectedRowVersion: null, ct);
        if (location is null)
            return WarehouseLocationErrors.NotFound;

        var changed = location.SetActive(command.IsActive);
        if (changed.IsFailure)
            return changed.Error;
        var saved = await warehouses.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ActiveChanged(logger, location.Id, command.IsActive);
        return Done.Value;
    }

    [LoggerMessage(EventId = 2011, Level = LogLevel.Information, Message = "Location {LocationId} active set to {IsActive}")]
    private static partial void ActiveChanged(ILogger logger, Guid locationId, bool isActive);
}
