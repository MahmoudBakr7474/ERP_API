namespace WarehouseERP.Accounting.Contracts;

/// <summary>Where a bank reconciliation is in its life.</summary>
public enum BankReconciliationStatus : byte
{
    /// <summary>Lines are being ticked against the statement.</summary>
    Draft = 1,

    /// <summary>The ticked lines equal the statement; they stay cleared.</summary>
    Completed = 2,

    /// <summary>Dropped; its lines are free again.</summary>
    Cancelled = 3,
}
