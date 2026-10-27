using WarehouseERP.Catalog.Contracts;
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

    /// <summary>How many base units one of this unit holds. Fixed once created: quantities already recorded rely on it.</summary>
    public decimal ConversionFactor { get; private set; }

    // Base unit is identified only by Product.BaseUnitId and has factor 1.

    public IReadOnlyCollection<ProductBarcode> Barcodes => _barcodes;

    internal static ProductUnit CreateBase(Product product, Guid unitId) =>
        new() { Product = product, UnitId = unitId, ConversionFactor = 1m };

    internal static ProductUnit Create(Product product, Guid unitId, decimal conversionFactor) =>
        new() { Product = product, UnitId = unitId, ConversionFactor = conversionFactor };

    internal ProductBarcode AddBarcode(string barcode, BarcodeType type)
    {
        var added = ProductBarcode.Create(this, barcode, type);
        _barcodes.Add(added);
        return added;
    }
}
