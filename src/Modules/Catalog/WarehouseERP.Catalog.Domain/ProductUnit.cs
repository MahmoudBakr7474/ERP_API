using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Catalog.Domain;

public sealed class ProductUnit : MasterEntity
{
    private readonly List<ProductBarcode> _barcodes = [];

    private ProductUnit() { } // EF Core

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public Guid UnitId { get; private set; }
    public Unit Unit { get; private set; } = null!;
    public decimal ConversionFactor { get; private set; }
    // Base unit is identified only by Product.BaseUnitId and has factor 1.
    public IReadOnlyCollection<ProductBarcode> Barcodes => _barcodes;

    internal static ProductUnit CreateBase(Product product, Guid unitId) =>
        new() { Product = product, UnitId = unitId, ConversionFactor = 1m };
}
