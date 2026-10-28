namespace WarehouseERP.Inventory.Contracts;

/// <summary>The condition stock is in; only available stock counts as sellable.</summary>
public enum StockStatus : byte
{
    /// <summary>Usable and sellable (in a physical warehouse).</summary>
    Available = 1,
}
