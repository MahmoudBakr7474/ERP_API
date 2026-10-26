using WarehouseERP.Catalog.Contracts;
using WarehouseERP.SharedKernel.Domain;
namespace WarehouseERP.Catalog.Domain;
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
    public ProductType ProductType { get; set; } = global::WarehouseERP.Catalog.Contracts.ProductType.StockItem;
    public TrackingMode TrackingMode { get; set; } = global::WarehouseERP.Catalog.Contracts.TrackingMode.None;
    public ICollection<ProductUnit> Units { get; set; } = new List<ProductUnit>();
}
