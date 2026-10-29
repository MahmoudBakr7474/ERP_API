using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Features.Shifts.ListPosShifts;

public sealed record ListPosShiftsQuery(
    int Page, int? PageSize, Guid? TerminalId, Guid? WarehouseId, Guid? CashierUserId, PosShiftStatus? Status) : IPagedQuery;
