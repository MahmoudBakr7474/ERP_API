namespace WarehouseERP.Accounting.Contracts;

/// <summary>Whether a treasury account is a cashbox or a bank account.</summary>
public enum CashAccountKind : byte
{
    /// <summary>A cashbox (خزينة); its balance never goes below zero.</summary>
    Cashbox = 1,

    /// <summary>A bank account; below zero only when it allows an overdraft.</summary>
    Bank = 2,
}
