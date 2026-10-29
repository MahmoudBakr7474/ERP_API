namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.Sales;

/// <param name="ScopeWarehouseIds">The user's warehouses; null for a company-wide user.</param>
/// <param name="Search">A receipt number, or part of one, or of the customer's name or phone.</param>
/// <param name="SoldFromUtc">Sold at or after.</param>
/// <param name="SoldBeforeUtc">Sold before.</param>
public sealed record PosSaleFilter(
    IReadOnlyCollection<Guid>? ScopeWarehouseIds, Guid? TerminalId, Guid? ShiftId, Guid? WarehouseId, Guid? CustomerId,
    Guid? CashierUserId, string? Search, DateTime? SoldFromUtc, DateTime? SoldBeforeUtc);
