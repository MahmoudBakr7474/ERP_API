namespace WarehouseERP.Purchasing.Domain;

/// <summary>A request line as entered, with its product unit's facts from Catalog.</summary>
public sealed record RequestLineInput(ProductUnitFacts Unit, decimal Quantity, string? Notes);
