namespace WarehouseERP.Accounting.Contracts;

/// <summary>Whether a fiscal year has been closed into retained earnings.</summary>
public enum FiscalYearStatus : byte
{
    /// <summary>Its periods open and close one by one.</summary>
    Open = 1,

    /// <summary>Every period is closed and its closing journal posted; reopening it reverses that journal.</summary>
    Closed = 2,
}
