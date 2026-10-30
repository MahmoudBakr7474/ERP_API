namespace WarehouseERP.Accounting.Contracts;

/// <summary>
/// The kind of party a control account's lines name. A control account (customers, suppliers) only takes lines that name
/// their customer or supplier, so its subledger always adds up to it.
/// </summary>
public enum PartyType : byte
{
    /// <summary>A Sales customer (receivables).</summary>
    Customer = 1,

    /// <summary>A Purchasing supplier (payables).</summary>
    Supplier = 2,
}
