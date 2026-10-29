using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Features.CardIncidents.ListPosCardIncidents;

public sealed record ListPosCardIncidentsQuery(int Page, int? PageSize, Guid? TerminalId, Guid? ShiftId, PosCardIncidentStatus? Status) : IPagedQuery;
