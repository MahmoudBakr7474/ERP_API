using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.CardIncidents;

/// <param name="ScopeWarehouseIds">The user's warehouses; null for a company-wide user.</param>
public sealed record CardIncidentFilter(
    IReadOnlyCollection<Guid>? ScopeWarehouseIds, Guid? TerminalId, Guid? ShiftId, PosCardIncidentStatus? Status);
