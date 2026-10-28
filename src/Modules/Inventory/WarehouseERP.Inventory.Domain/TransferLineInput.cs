namespace WarehouseERP.Inventory.Domain;

/// <summary>A transfer line as entered, with the product unit's facts the application looked up in Catalog.</summary>
/// <param name="SourceLocationId">Where in the source warehouse the stock is picked from.</param>
public sealed record TransferLineInput(
    Guid ProductId, Guid ProductUnitId, Guid UnitId, decimal ConversionFactor, bool UnitAllowsFraction,
    bool BaseUnitAllowsFraction, decimal Quantity, Guid SourceLocationId);
