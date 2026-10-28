namespace WarehouseERP.Purchasing.Contracts;

/// <summary>Where a purchase request is in its life.</summary>
public enum PurchaseRequestStatus : byte
{
    /// <summary>Being prepared; its lines can change.</summary>
    Draft = 1,

    /// <summary>Waiting for approval.</summary>
    Submitted = 2,

    /// <summary>Approved; its remaining quantities may be ordered.</summary>
    Approved = 3,

    /// <summary>Every line was ordered in full.</summary>
    Ordered = 4,

    /// <summary>Closed by hand; what was not ordered never will be.</summary>
    Closed = 5,

    /// <summary>Abandoned before anything was ordered.</summary>
    Cancelled = 6,
}
