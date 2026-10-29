namespace WarehouseERP.Sales.Contracts;

/// <summary>A product unit to look up in the price list, with its product (whose tax classification applies).</summary>
public sealed record SalesProductUnit(Guid ProductId, Guid ProductUnitId);
