namespace WarehouseERP.Accounting.Contracts;

/// <summary>Where a posting account appears in the balance sheet or the income statement; it must fit the account's type.</summary>
public enum StatementSection : byte
{
    /// <summary>Asset expected to turn into cash within a year (inventory, receivables, cash).</summary>
    CurrentAssets = 1,

    /// <summary>Long-term asset (fixed assets, their accumulated depreciation).</summary>
    NonCurrentAssets = 2,

    /// <summary>Liability due within a year (suppliers, taxes, accruals).</summary>
    CurrentLiabilities = 3,

    /// <summary>Liability due after a year (long-term loans).</summary>
    NonCurrentLiabilities = 4,

    /// <summary>Capital, reserves and retained earnings.</summary>
    Equity = 5,

    /// <summary>Sales and their returns.</summary>
    Revenue = 6,

    /// <summary>Cost of goods sold and the inventory differences counted with it.</summary>
    CostOfSales = 7,

    /// <summary>Selling, distribution, general and administrative expenses.</summary>
    OperatingExpenses = 8,

    /// <summary>Income outside trading (gains, interest, miscellaneous).</summary>
    OtherIncome = 9,

    /// <summary>Expenses outside operations (losses, finance costs).</summary>
    OtherExpenses = 10,

    /// <summary>Income tax on the year's profit.</summary>
    IncomeTax = 11,
}
