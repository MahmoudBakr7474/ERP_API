namespace WarehouseERP.Inventory.Contracts;

/// <summary>Why an adjustment changes stock.</summary>
public enum AdjustmentReason : byte
{
    /// <summary>Goods damaged in the warehouse.</summary>
    Damage = 1,

    /// <summary>Goods lost or stolen.</summary>
    Loss = 2,

    /// <summary>Goods past their expiry date.</summary>
    Expiry = 3,

    /// <summary>Goods found that were not recorded.</summary>
    Found = 4,

    /// <summary>A correction of an earlier mistake.</summary>
    Correction = 5,

    /// <summary>Anything else; the notes say what.</summary>
    Other = 6,
}
