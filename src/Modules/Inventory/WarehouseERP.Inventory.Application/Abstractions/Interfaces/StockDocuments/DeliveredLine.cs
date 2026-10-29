namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;

/// <summary>A line of a posted sales delivery or POS sale, which a customer return may bring goods back from.</summary>
public sealed record DeliveredLine(Guid LineId, Guid ProductId, Guid WarehouseId);
