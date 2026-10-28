namespace WarehouseERP.Purchasing.Contracts;

/// <summary>Where a supplier invoice is in its life.</summary>
public enum SupplierInvoiceStatus : byte
{
    /// <summary>Being entered; its lines can change.</summary>
    Draft = 1,

    /// <summary>Its prices differ from the order and it waits for approval.</summary>
    Submitted = 2,

    /// <summary>Recorded as a liability to the supplier, numbered and immutable.</summary>
    Posted = 3,

    /// <summary>Abandoned before posting.</summary>
    Cancelled = 4,
}
