using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Catalog.Domain;

public sealed class ProductBarcode : MasterEntity
{
    public Guid ProductUnitId { get; set; }
    public ProductUnit ProductUnit { get; set; } = null!;
    public string Barcode { get; set; } = null!;
    public BarcodeType BarcodeType { get; set; } = global::WarehouseERP.Catalog.Domain.BarcodeType.Internal;
}
