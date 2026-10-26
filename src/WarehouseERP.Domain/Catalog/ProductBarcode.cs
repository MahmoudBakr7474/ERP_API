using WarehouseERP.Domain.Common;
namespace WarehouseERP.Domain.Catalog;
public enum BarcodeType : byte { Internal = 1, Ean13 = 2, Gtin = 3, Other = 4 }
public sealed class ProductBarcode : MasterEntity
{
    public Guid ProductUnitId { get; set; }
    public ProductUnit ProductUnit { get; set; } = null!;
    public string Barcode { get; set; } = null!;
    public BarcodeType BarcodeType { get; set; } = global::WarehouseERP.Domain.Catalog.BarcodeType.Internal;
}
