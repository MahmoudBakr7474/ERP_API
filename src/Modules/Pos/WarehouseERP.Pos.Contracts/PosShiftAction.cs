namespace WarehouseERP.Pos.Contracts;

/// <summary>One step in a shift's history.</summary>
public enum PosShiftAction : byte
{
    /// <summary>Opened with its float.</summary>
    Opened = 1,

    /// <summary>Cash put into the drawer.</summary>
    CashIn = 2,

    /// <summary>Cash taken out of the drawer.</summary>
    CashOut = 3,

    /// <summary>Counted and closed by its cashier.</summary>
    Closed = 4,

    /// <summary>Counted and closed by a shift manager instead of its cashier.</summary>
    ForceClosed = 5,
}
