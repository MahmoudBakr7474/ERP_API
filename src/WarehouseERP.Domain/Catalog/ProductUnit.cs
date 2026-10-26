using WarehouseERP.Domain.Common;
namespace WarehouseERP.Domain.Catalog;
public sealed class ProductUnit : MasterEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
    public decimal ConversionFactor { get; set; }
    // Base unit is identified only by Product.BaseUnitId and has factor 1.
    public ICollection<ProductBarcode> Barcodes { get; set; } = new List<ProductBarcode>();
}
