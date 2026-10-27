using WarehouseERP.Catalog.Contracts;
using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Catalog.Domain;

public sealed class ProductBarcode : MasterEntity
{
    private ProductBarcode() { } // EF Core

    public Guid ProductUnitId { get; private set; }

    public ProductUnit ProductUnit { get; private set; } = null!;

    /// <summary>Fixed once created. A retired barcode keeps its value reserved (the unique index covers every row).</summary>
    public string Barcode { get; private set; } = null!;

    public BarcodeType BarcodeType { get; private set; } = BarcodeType.Internal;

    // Created through Product.AddBarcode, which checks the format and that the product and its unit are active.
    internal static ProductBarcode Create(ProductUnit productUnit, string barcode, BarcodeType type) =>
        new() { ProductUnit = productUnit, Barcode = barcode, BarcodeType = type };
}
