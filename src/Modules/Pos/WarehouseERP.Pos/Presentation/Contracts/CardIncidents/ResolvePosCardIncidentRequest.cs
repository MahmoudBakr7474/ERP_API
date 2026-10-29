namespace WarehouseERP.Pos.Presentation.Contracts.CardIncidents;

/// <summary>How the card charge was settled, and the incident's row version.</summary>
public sealed record ResolvePosCardIncidentRequest(string? Note, string RowVersion);
