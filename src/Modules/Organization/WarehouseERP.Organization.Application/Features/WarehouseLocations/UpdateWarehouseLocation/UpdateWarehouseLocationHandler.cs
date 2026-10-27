using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.WarehouseLocations.UpdateWarehouseLocation;

// The code is fixed; only the names change. Returns the new row version for the caller's next update.
public sealed partial class UpdateWarehouseLocationHandler(
    IWarehouseRepository warehouses, ICurrentUser currentUser, ILogger<UpdateWarehouseLocationHandler> logger)
    : ICommandHandler<UpdateWarehouseLocationCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateWarehouseLocationCommand command, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(command.WarehouseId))
            return WarehouseErrors.AccessDenied;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var location = await warehouses.GetLocationForUpdateAsync(command.WarehouseId, command.LocationId, expected, ct);
        if (location is null)
            return WarehouseLocationErrors.NotFound;

        location.Rename(command.NameAr, command.NameEn);
        var saved = await warehouses.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        LocationRenamed(logger, location.Id);
        return Convert.ToBase64String(location.RowVersion);
    }

    [LoggerMessage(EventId = 2010, Level = LogLevel.Information, Message = "Location {LocationId} renamed")]
    private static partial void LocationRenamed(ILogger logger, Guid locationId);
}
