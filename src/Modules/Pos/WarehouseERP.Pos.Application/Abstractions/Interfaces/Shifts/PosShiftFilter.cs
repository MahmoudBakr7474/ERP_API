using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.Shifts;

/// <param name="ScopeWarehouseIds">The user's warehouses; null for a company-wide user.</param>
public sealed record PosShiftFilter(
    IReadOnlyCollection<Guid>? ScopeWarehouseIds, Guid? TerminalId, Guid? WarehouseId, Guid? CashierUserId, PosShiftStatus? Status);
