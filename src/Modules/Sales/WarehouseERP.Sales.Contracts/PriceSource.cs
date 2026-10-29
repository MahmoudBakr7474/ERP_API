namespace WarehouseERP.Sales.Contracts;

/// <summary>Where a line's unit price came from; the price is a snapshot either way.</summary>
public enum PriceSource : byte
{
    /// <summary>The sales price list's price for the line's product unit when the line was entered.</summary>
    PriceList = 1,

    /// <summary>A price set by a user allowed to manage prices, where the list had none or instead of it.</summary>
    Manual = 2,
}
