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

    /// <summary>Money received from a customer into a cashbox or bank, net of any tax the customer withheld (CR).</summary>
    CustomerReceipt = 5,

    /// <summary>Money paid to a supplier from a cashbox or bank, net of any tax the company withheld (SP).</summary>
    SupplierPayment = 6,

    /// <summary>Money paid back to a customer out of their credit (CF).</summary>
    CustomerRefund = 7,

    /// <summary>Money a supplier paid back out of what they owed the company (SF).</summary>
    SupplierRefund = 8,

    /// <summary>Money received into a cashbox or bank against other accounts (RV).</summary>
    CashReceipt = 9,

    /// <summary>Money paid from a cashbox or bank against other accounts, such as expenses (PV).</summary>
    CashPayment = 10,

    /// <summary>Money moved between two cashboxes or banks (TR).</summary>
    CashTransfer = 11,

    /// <summary>A supplier's bill for expenses or services (not stock), with recoverable VAT (EB).</summary>
    ExpenseBill = 12,
}
