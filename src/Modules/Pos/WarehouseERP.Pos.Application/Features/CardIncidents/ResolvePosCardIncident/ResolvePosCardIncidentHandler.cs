using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.CardIncidents;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.CardIncidents.ResolvePosCardIncident;

// A shift manager (pos.shifts.manage, checked by the endpoint) confirms how a card charge without a sale was settled.
public sealed partial class ResolvePosCardIncidentHandler(
    ICardIncidentRepository incidents, ICurrentUser currentUser, TimeProvider clock, ILogger<ResolvePosCardIncidentHandler> logger)
    : ICommandHandler<ResolvePosCardIncidentCommand, string>
{
    public async Task<Result<string>> HandleAsync(ResolvePosCardIncidentCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = PosAccess.Check(currentUser, await incidents.FindWarehouseAsync(command.IncidentId, ct), CardIncidentErrors.NotFound);
        if (access.IsFailure)
            return access.Error;
        var incident = await incidents.GetForUpdateAsync(command.IncidentId, expected, ct);
        if (incident is null)
            return CardIncidentErrors.NotFound;
        var resolved = incident.Resolve(command.Note, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (resolved.IsFailure)
            return resolved.Error;
        var saved = await incidents.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        IncidentResolved(logger, incident.Id);
        return Convert.ToBase64String(incident.RowVersion);
    }

    [LoggerMessage(EventId = 8050, Level = LogLevel.Information, Message = "POS card incident {IncidentId} resolved")]
    private static partial void IncidentResolved(ILogger logger, Guid incidentId);
}
