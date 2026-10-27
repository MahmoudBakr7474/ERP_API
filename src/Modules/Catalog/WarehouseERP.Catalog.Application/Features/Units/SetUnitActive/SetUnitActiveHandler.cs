using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Units;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Units.SetUnitActive;

// A unit is retired after the active products that use it, so an active product never stands on an inactive unit.
public sealed partial class SetUnitActiveHandler(IUnitRepository units, ILogger<SetUnitActiveHandler> logger)
    : ICommandHandler<SetUnitActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetUnitActiveCommand command, CancellationToken ct)
    {
        var unit = await units.GetForUpdateAsync(command.UnitId, expectedRowVersion: null, ct);
        if (unit is null)
            return UnitErrors.NotFound;
        if (!command.IsActive && await units.IsUsedByActiveProductsAsync(unit.Id, ct))
            return UnitErrors.InUse;

        if (command.IsActive) unit.Activate();
        else unit.Deactivate();
        var saved = await units.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ActiveChanged(logger, unit.Id, command.IsActive);
        return Done.Value;
    }

    [LoggerMessage(EventId = 3005, Level = LogLevel.Information, Message = "Unit {UnitId} active set to {IsActive}")]
    private static partial void ActiveChanged(ILogger logger, Guid unitId, bool isActive);
}
