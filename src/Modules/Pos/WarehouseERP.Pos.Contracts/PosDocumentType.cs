namespace WarehouseERP.Pos.Contracts;

/// <summary>What a POS number series numbers; each terminal has its own series per type and year.</summary>
public enum PosDocumentType : byte
{
    /// <summary>Cashier shifts (CAI-T1-SH-2026-000001).</summary>
    Shift = 1,

    /// <summary>Sales receipts (CAI-T1-2026-000001).</summary>
    Sale = 2,

    /// <summary>Returns (CAI-T1-R-2026-000001).</summary>
    Return = 3,
}
