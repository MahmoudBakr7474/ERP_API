namespace WarehouseERP.Sales.Contracts;

/// <summary>Why a quotation or an order needs approval before it is issued or confirmed.</summary>
public enum ApprovalReason : byte
{
    /// <summary>The sales settings require approval for every document of its type.</summary>
    Policy = 1,

    /// <summary>A line's discount is above the settings' approval threshold.</summary>
    DiscountAboveThreshold = 2,

    /// <summary>A line's manual price is below the price list, and the settings want such prices approved.</summary>
    PriceBelowList = 3,

    /// <summary>A line has a manual price, and the settings want manual prices approved.</summary>
    ManualPrice = 4,

    /// <summary>The customer is on credit hold, and the settings want such orders approved instead of refused.</summary>
    CreditHold = 5,
}
