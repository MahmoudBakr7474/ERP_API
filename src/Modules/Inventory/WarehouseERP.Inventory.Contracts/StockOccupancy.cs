namespace WarehouseERP.Inventory.Contracts;

/// <summary>What still ties a warehouse or a location to Inventory.</summary>
public enum StockOccupancy
{
    /// <summary>No stock and no open documents: it may be deactivated.</summary>
    Free = 0,

    /// <summary>It holds stock (a non-zero balance in any status).</summary>
    HoldsStock = 1,

    /// <summary>A submitted document waiting for approval would post into it.</summary>
    HasOpenDocuments = 2,

    /// <summary>Inventory's lock could not be taken in time; try again.</summary>
    Busy = 3,
}
