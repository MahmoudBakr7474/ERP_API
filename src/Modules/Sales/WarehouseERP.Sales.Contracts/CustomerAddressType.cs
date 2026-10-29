namespace WarehouseERP.Sales.Contracts;

/// <summary>What a customer's address is used for.</summary>
public enum CustomerAddressType : byte
{
    /// <summary>Where invoices go.</summary>
    Billing = 1,

    /// <summary>Where goods are delivered.</summary>
    Shipping = 2,

    /// <summary>Any other address.</summary>
    Other = 3,
}
