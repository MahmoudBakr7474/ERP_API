namespace WarehouseERP.Pos.Application.Contracts.Sales;

/// <summary>A cart line: a product unit, a quantity in it and a line discount in percent (0 unless allowed).</summary>
public sealed record PosCartLineRequest(Guid ProductId, Guid ProductUnitId, decimal Quantity, decimal DiscountPercent);
