using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Pos.Application.Features.Returns.ListPosReturns;

/// <param name="Search">A return or receipt number, or part of one.</param>
public sealed record ListPosReturnsQuery(
    int Page, int? PageSize, Guid? TerminalId, Guid? ShiftId, Guid? SaleId, Guid? WarehouseId, string? Search, DateOnly? From,
    DateOnly? To) : IPagedQuery;
