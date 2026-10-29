namespace WarehouseERP.Pos.Contracts;

/// <summary>Where a cashier shift is in its life.</summary>
public enum PosShiftStatus : byte
{
    /// <summary>The cashier sells on it; its drawer changes.</summary>
    Open = 1,

    /// <summary>Counted and closed; it never changes again.</summary>
    Closed = 2,
}
