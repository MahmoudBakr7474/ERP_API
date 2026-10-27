using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Units;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Units.CreateUnit;

// One save point (IUnitRepository.AddAsync); the pre-check gives a clear error and the database still guards the
// code if two requests race.
public sealed partial class CreateUnitHandler(IUnitRepository units, ILogger<CreateUnitHandler> logger)
    : ICommandHandler<CreateUnitCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateUnitCommand command, CancellationToken ct)
    {
        if (await units.CodeExistsAsync(command.Code.Trim(), ct))
            return UnitErrors.CodeTaken;

        var unit = Unit.Create(command.Code, command.NameAr, command.NameEn, command.AllowsFraction);
        var saved = await units.AddAsync(unit, ct);
        if (saved.IsFailure)
            return saved.Error;

        UnitCreated(logger, unit.Id, unit.Code);
        return unit.Id;
    }

    [LoggerMessage(EventId = 3003, Level = LogLevel.Information, Message = "Unit {UnitId} ({UnitCode}) created")]
    private static partial void UnitCreated(ILogger logger, Guid unitId, string unitCode);
}
