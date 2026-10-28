namespace WarehouseERP.Inventory.Contracts;

/// <summary>Whether a document line brings stock in or takes it out.</summary>
public enum StockDirection : byte
{
    /// <summary>Adds stock to the line's location.</summary>
    In = 1,

    /// <summary>Removes stock from the line's location.</summary>
    Out = 2,
}
