namespace WarehouseERP.Sales.Contracts;

/// <summary>A kind of sales document; each has its own number series per branch and fiscal year.</summary>
public enum SalesDocumentType : byte
{
    /// <summary>A quotation (QT).</summary>
    Quotation = 1,

    /// <summary>A sales order (SO).</summary>
    Order = 2,

    /// <summary>A delivery note (DN).</summary>
    Delivery = 3,

    /// <summary>A sales invoice (INV).</summary>
    Invoice = 4,

    /// <summary>A customer return (SR).</summary>
    Return = 5,
}
