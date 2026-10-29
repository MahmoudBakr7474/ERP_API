using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Pos.Application.Shared;

/// <summary>A cart line's product and product unit as Catalog has them.</summary>
public sealed record PosUnit(ProductInfo Product, ProductUnitInfo Unit);
