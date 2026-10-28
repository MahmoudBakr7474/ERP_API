namespace WarehouseERP.Purchasing.Contracts;

/// <summary>Where a supplier return is in its life.</summary>
public enum SupplierReturnStatus : byte
{
    /// <summary>Being prepared; its lines can change.</summary>
    Draft = 1,

    /// <summary>Waiting for approval.</summary>
    Submitted = 2,

    /// <summary>Approved: the goods left stock and the return is numbered and immutable.</summary>
    Posted = 3,

    /// <summary>Abandoned before posting.</summary>
    Cancelled = 4,
}
