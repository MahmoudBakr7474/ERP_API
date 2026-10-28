namespace WarehouseERP.Inventory.Domain;

/// <summary>What one valuation step did: signed base quantity and value, and the unit cost per base unit it used.</summary>
public readonly record struct CostChange(decimal QuantityDelta, decimal ValueDelta, decimal UnitCost);
