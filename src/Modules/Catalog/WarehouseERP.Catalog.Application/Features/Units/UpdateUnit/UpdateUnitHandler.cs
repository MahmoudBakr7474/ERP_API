using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Units;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.Catalog.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Units.UpdateUnit;

// The code is fixed. Returns the new row version for the caller's next update.
public sealed partial class UpdateUnitHandler(IUnitRepository units, ILogger<UpdateUnitHandler> logger)
    : ICommandHandler<UpdateUnitCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateUnitCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var unit = await units.GetForUpdateAsync(command.UnitId, expected, ct);
        if (unit is null)
            return UnitErrors.NotFound;

        unit.Update(command.NameAr, command.NameEn, command.AllowsFraction);
        var saved = await units.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        UnitUpdated(logger, unit.Id);
        return Convert.ToBase64String(unit.RowVersion);
    }

    [LoggerMessage(EventId = 3004, Level = LogLevel.Information, Message = "Unit {UnitId} updated")]
    private static partial void UnitUpdated(ILogger logger, Guid unitId);
}
