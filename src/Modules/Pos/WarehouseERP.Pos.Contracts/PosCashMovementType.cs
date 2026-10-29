namespace WarehouseERP.Pos.Contracts;

/// <summary>Cash put into or taken out of a shift's drawer outside sales and refunds.</summary>
public enum PosCashMovementType : byte
{
    /// <summary>Cash added to the drawer (for example more change).</summary>
    CashIn = 1,

    /// <summary>Cash taken out of the drawer (for example a safe drop).</summary>
    CashOut = 2,
}
