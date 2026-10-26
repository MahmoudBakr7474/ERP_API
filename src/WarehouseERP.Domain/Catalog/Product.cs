using WarehouseERP.Domain.Common;
namespace WarehouseERP.Domain.Catalog;
public enum ProductType : byte { StockItem = 1, NonStockItem = 2, Service = 3 }
public enum TrackingMode : byte { None = 0, Batch = 1, Serial = 2 }
public sealed class Product : MasterEntity
{
    public string SKU { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    // Single source of truth; ProductUnit has no IsBaseUnit flag.
    public Guid BaseUnitId { get; set; }
    public Unit BaseUnit { get; set; } = null!;
    public ProductType ProductType { get; set; } = global::WarehouseERP.Domain.Catalog.ProductType.StockItem;
    public TrackingMode TrackingMode { get; set; } = global::WarehouseERP.Domain.Catalog.TrackingMode.None;
    public ICollection<ProductUnit> Units { get; set; } = new List<ProductUnit>();
}
