namespace WarehouseERP.Pos.Application.Features.CardIncidents.ResolvePosCardIncident;

/// <param name="Note">How the charge was settled (for example: reversed on the card terminal, reference 123456).</param>
public sealed record ResolvePosCardIncidentCommand(Guid IncidentId, string? Note, string RowVersion);
