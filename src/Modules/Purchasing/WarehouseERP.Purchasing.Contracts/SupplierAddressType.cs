namespace WarehouseERP.Purchasing.Contracts;

/// <summary>What a supplier's address is used for.</summary>
public enum SupplierAddressType : byte
{
    /// <summary>Where invoices come from.</summary>
    Billing = 1,

    /// <summary>Where goods are shipped from.</summary>
    Shipping = 2,

    /// <summary>Any other address.</summary>
    Other = 3,
}
