namespace WarehouseERP.Inventory.Domain;

/// <summary>A counted quantity in base units, with the base unit's fraction rule the application looked up.</summary>
/// <param name="UnitCost">Cost of one base unit for a surplus of a product that was never valued; otherwise optional.</param>
public sealed record CountEntry(Guid ProductId, Guid LocationId, decimal CountedQuantity, decimal? UnitCost, bool BaseUnitAllowsFraction);
