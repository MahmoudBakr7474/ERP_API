namespace WarehouseERP.Accounting.Contracts;

/// <summary>Where a journal comes from; each type has its own number series per fiscal year.</summary>
public enum JournalType : byte
{
    /// <summary>A journal an accountant writes (GJ).</summary>
    Manual = 1,

    /// <summary>Balances the ledger starts with on the accounting start date; always approved (OB).</summary>
    OpeningBalance = 2,

    /// <summary>Undoes a posted journal line by line on its own date (RJ).</summary>
    Reversal = 3,

    /// <summary>Moves a closed fiscal year's revenue and expenses into retained earnings (CJ).</summary>
    YearEndClosing = 4,
}
