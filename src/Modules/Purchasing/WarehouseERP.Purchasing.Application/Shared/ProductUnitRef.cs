namespace WarehouseERP.Purchasing.Application.Shared;

/// <summary>A line's product and product unit, to look up in Catalog.</summary>
public sealed record ProductUnitRef(Guid ProductId, Guid ProductUnitId);
