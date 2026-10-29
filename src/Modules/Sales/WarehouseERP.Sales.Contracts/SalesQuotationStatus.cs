namespace WarehouseERP.Sales.Contracts;

/// <summary>Where a quotation is in its life. An issued quotation past its validity date shows as expired.</summary>
public enum SalesQuotationStatus : byte
{
    /// <summary>Being prepared; lines may change.</summary>
    Draft = 1,

    /// <summary>Waiting for an approver.</summary>
    Submitted = 2,

    /// <summary>Numbered and given to the customer.</summary>
    Issued = 3,

    /// <summary>The customer accepted it: it became a sales order.</summary>
    Converted = 4,

    /// <summary>The customer declined it.</summary>
    Declined = 5,

    /// <summary>Abandoned.</summary>
    Cancelled = 6,
}
