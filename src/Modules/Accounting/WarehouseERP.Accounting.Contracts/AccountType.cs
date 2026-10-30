namespace WarehouseERP.Accounting.Contracts;

/// <summary>The class of an account; it decides its normal balance and the statement it reports on.</summary>
public enum AccountType : byte
{
    /// <summary>What the company owns; normal debit balance (balance sheet).</summary>
    Asset = 1,

    /// <summary>What the company owes; normal credit balance (balance sheet).</summary>
    Liability = 2,

    /// <summary>The owners' interest; normal credit balance (balance sheet).</summary>
    Equity = 3,

    /// <summary>Income earned; normal credit balance (income statement).</summary>
    Revenue = 4,

    /// <summary>Costs and expenses; normal debit balance (income statement).</summary>
    Expense = 5,
}
