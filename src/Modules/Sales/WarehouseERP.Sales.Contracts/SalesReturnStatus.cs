namespace WarehouseERP.Sales.Contracts;

/// <summary>Where a customer return is in its life.</summary>
public enum SalesReturnStatus : byte
{
    /// <summary>Being prepared; lines may change.</summary>
    Draft = 1,

    /// <summary>Waiting for an approver.</summary>
    Submitted = 2,

    /// <summary>Approved: numbered, the goods are back in stock and any credit owed to the customer is recorded.</summary>
    Approved = 3,

    /// <summary>Abandoned before approval.</summary>
    Cancelled = 4,
}
