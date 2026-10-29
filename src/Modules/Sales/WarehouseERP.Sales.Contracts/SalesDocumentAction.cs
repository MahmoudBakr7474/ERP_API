namespace WarehouseERP.Sales.Contracts;

/// <summary>A step in a sales document's history.</summary>
public enum SalesDocumentAction : byte
{
    /// <summary>The draft was created.</summary>
    Created = 1,

    /// <summary>It was submitted for approval (the note lists why it needs it).</summary>
    Submitted = 2,

    /// <summary>An approver sent it back to draft, with a reason.</summary>
    Rejected = 3,

    /// <summary>An approver approved it.</summary>
    Approved = 4,

    /// <summary>It was abandoned (with a reason when given).</summary>
    Cancelled = 5,

    /// <summary>What remained of an order was closed, with a reason.</summary>
    Closed = 6,

    /// <summary>It was posted and numbered.</summary>
    Posted = 7,

    /// <summary>A quotation was issued to the customer and numbered.</summary>
    Issued = 8,

    /// <summary>The customer declined a quotation, with a reason.</summary>
    Declined = 9,

    /// <summary>A quotation became a sales order (the note names it).</summary>
    Converted = 10,

    /// <summary>An order was confirmed, numbered and its stock reserved.</summary>
    Confirmed = 11,

    /// <summary>A delivery of the order was posted (the note names it).</summary>
    Delivered = 12,

    /// <summary>An invoice for the order was posted (the note names it).</summary>
    Invoiced = 13,

    /// <summary>Delivered goods came back (the note names the customer return).</summary>
    Returned = 14,

    /// <summary>An order line's quantity was reduced and its reservation released, with a reason.</summary>
    Reduced = 15,
}
