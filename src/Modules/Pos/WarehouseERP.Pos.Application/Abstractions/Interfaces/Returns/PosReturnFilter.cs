namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.Returns;

/// <param name="ScopeWarehouseIds">The user's warehouses; null for a company-wide user.</param>
/// <param name="Search">A return or receipt number, or part of one.</param>
public sealed record PosReturnFilter(
    IReadOnlyCollection<Guid>? ScopeWarehouseIds, Guid? TerminalId, Guid? ShiftId, Guid? SaleId, Guid? WarehouseId, string? Search,
    DateTime? ReturnedFromUtc, DateTime? ReturnedBeforeUtc);
