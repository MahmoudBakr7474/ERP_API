namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;

/// <summary>
/// What a posted sales delivery line took out of stock at cost, and how much of it posted customer returns brought back;
/// quantities in base units, values in the base currency, all positive.
/// </summary>
public sealed record DeliveryCost(decimal IssuedQuantity, decimal IssuedValue, decimal ReturnedQuantity, decimal ReturnedValue);
