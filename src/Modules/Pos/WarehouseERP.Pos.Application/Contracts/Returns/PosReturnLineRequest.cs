using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Contracts.Returns;

/// <summary>A quantity of a sale line coming back, in the line's unit, and its condition (only fit for resale is accepted).</summary>
public sealed record PosReturnLineRequest(Guid SaleLineId, decimal Quantity, PosReturnCondition Condition);
