using WarehouseERP.Catalog.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Domain;

public sealed class Product : MasterEntity
{
    private readonly List<ProductUnit> _units = [];

    private Product() { } // EF Core

    public string SKU { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Category? Category { get; private set; }
    // Single source of truth; ProductUnit has no IsBaseUnit flag.
    public Guid BaseUnitId { get; private set; }
    public Unit BaseUnit { get; private set; } = null!;
    public ProductType ProductType { get; private set; }
    public TrackingMode TrackingMode { get; private set; }
    public IReadOnlyCollection<ProductUnit> Units => _units;

    /// <summary>
    /// A new product always comes with its base unit row (factor 1), saved with it in one SaveChanges.
    /// Only stock items can be tracked by batch or serial.
    /// </summary>
    public static Result<Product> Create(
        string sku, string nameAr, string? nameEn, Guid baseUnitId, Guid? categoryId, ProductType type, TrackingMode tracking)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        if (type != ProductType.StockItem && tracking != TrackingMode.None)
            return ProductDomainErrors.TrackingNeedsStockItem;

        var product = new Product
        {
            SKU = sku.Trim(),
            NameAr = nameAr.Trim(),
            NameEn = Text.Optional(nameEn),
            BaseUnitId = baseUnitId,
            CategoryId = categoryId,
            ProductType = type,
            TrackingMode = tracking,
        };
        product._units.Add(ProductUnit.CreateBase(product, baseUnitId));
        return product;
    }

    public void Rename(string nameAr, string? nameEn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        NameAr = nameAr.Trim();
        NameEn = Text.Optional(nameEn);
    }
}
