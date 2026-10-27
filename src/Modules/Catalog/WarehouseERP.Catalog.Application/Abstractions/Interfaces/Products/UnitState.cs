namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;

/// <summary>What a product command needs to know about a unit it names.</summary>
public sealed record UnitState(bool IsActive, bool AllowsFraction);
