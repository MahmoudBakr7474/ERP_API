namespace WarehouseERP.Sales.Contracts;

/// <summary>
/// The state returned goods arrive in. Only goods confirmed fit for resale go back into sellable stock in V1; the other
/// conditions are refused explicitly (Inventory has no quarantine or non-sellable stock yet), never restocked.
/// </summary>
public enum ReturnCondition : byte
{
    /// <summary>Inspected and fit for resale: back into sellable stock.</summary>
    FitForResale = 1,

    /// <summary>Damaged or defective: not supported yet.</summary>
    Damaged = 2,

    /// <summary>Not inspected yet: not supported yet.</summary>
    Uninspected = 3,
}
