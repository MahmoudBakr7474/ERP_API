using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Pos.Application.Features.Sales.ListPosSales;

/// <param name="Search">A receipt number, or part of one, or of the customer's name or phone.</param>
/// <param name="From">Sold on or after this business date (company time zone).</param>
/// <param name="To">Sold on or before this business date.</param>
public sealed record ListPosSalesQuery(
    int Page, int? PageSize, Guid? TerminalId, Guid? ShiftId, Guid? WarehouseId, Guid? CustomerId, Guid? CashierUserId, string? Search,
    DateOnly? From, DateOnly? To) : IPagedQuery;
