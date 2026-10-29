namespace WarehouseERP.Sales.Contracts;

/// <summary>Where a sales invoice is in its life.</summary>
public enum SalesInvoiceStatus : byte
{
    /// <summary>Being prepared; lines may change.</summary>
    Draft = 1,

    /// <summary>Numbered and immutable: the amount the customer owes is recorded.</summary>
    Posted = 2,

    /// <summary>Abandoned before posting.</summary>
    Cancelled = 3,
}
