namespace WarehouseERP.Pos.Contracts;

/// <summary>The state goods come back in. V1 takes back only goods fit for resale; anything else is refused.</summary>
public enum PosReturnCondition : byte
{
    /// <summary>Back into sellable stock at the cost it was sold with.</summary>
    FitForResale = 1,

    /// <summary>Damaged; refused until Inventory holds non-sellable stock.</summary>
    Damaged = 2,

    /// <summary>Not inspected yet; refused until Inventory holds non-sellable stock.</summary>
    Uninspected = 3,
}
