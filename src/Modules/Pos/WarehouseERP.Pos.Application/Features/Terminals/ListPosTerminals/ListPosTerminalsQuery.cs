using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Pos.Application.Features.Terminals.ListPosTerminals;

/// <param name="Search">Part of a terminal's code or name.</param>
public sealed record ListPosTerminalsQuery(int Page, int? PageSize, Guid? WarehouseId, bool? IsActive, string? Search) : IPagedQuery;
