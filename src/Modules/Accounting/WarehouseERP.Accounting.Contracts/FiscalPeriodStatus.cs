namespace WarehouseERP.Accounting.Contracts;

/// <summary>Whether journals may still post into a period.</summary>
public enum FiscalPeriodStatus : byte
{
    /// <summary>Journals dated in it may post.</summary>
    Open = 1,

    /// <summary>Nothing posts into it until it is reopened.</summary>
    Closed = 2,
}
