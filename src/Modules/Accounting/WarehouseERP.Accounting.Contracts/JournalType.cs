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

    /// <summary>Money received from a customer into a cashbox or bank, net of any tax the customer withheld (RV).</summary>
    CustomerReceipt = 5,

    /// <summary>Money paid to a supplier from a cashbox or bank, net of any tax the company withheld (PV).</summary>
    SupplierPayment = 6,

    /// <summary>Money paid back to a customer out of their credit (CF).</summary>
    CustomerRefund = 7,

    /// <summary>Money a supplier paid back out of what they owed the company (SF).</summary>
    SupplierRefund = 8,

    /// <summary>Money received into a cashbox or bank against other accounts (CR).</summary>
    CashReceipt = 9,

    /// <summary>Money paid from a cashbox or bank against other accounts, such as expenses (CP).</summary>
    CashPayment = 10,

    /// <summary>Money moved between two cashboxes or banks (TR).</summary>
    CashTransfer = 11,

    /// <summary>A supplier's bill for expenses or services (not stock), with recoverable VAT (EB).</summary>
    ExpenseBill = 12,

    /// <summary>The system's journal of an Inventory stock document: opening stock, receipt, issue, adjustment, count (IV).</summary>
    InventoryJournal = 13,

    /// <summary>The system's journal of a goods receipt, supplier invoice or supplier return (PU).</summary>
    PurchasingJournal = 14,

    /// <summary>The system's journal of a sales delivery, invoice or customer return (SL).</summary>
    SalesJournal = 15,

    /// <summary>The system's journal of a POS sale, return or shift close (PS).</summary>
    PosJournal = 16,
}
