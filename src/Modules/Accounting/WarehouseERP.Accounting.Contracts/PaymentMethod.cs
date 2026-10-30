namespace WarehouseERP.Accounting.Contracts;

/// <summary>How money moved, kept on receipts, payments and refunds.</summary>
public enum PaymentMethod : byte
{
    /// <summary>Cash into or out of a cashbox.</summary>
    Cash = 1,

    /// <summary>A transfer into or out of a bank account.</summary>
    BankTransfer = 2,

    /// <summary>
    /// A cheque, recorded only once it has cleared in the bank: V1 keeps no portfolio of cheques waiting to clear, and an
    /// uncleared cheque is never treated as bank cash.
    /// </summary>
    Cheque = 3,

    /// <summary>A card settlement into a bank account.</summary>
    Card = 4,
}
