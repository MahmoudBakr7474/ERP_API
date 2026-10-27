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

    /// <summary>
    /// The names and category change; SKU, type, tracking and base unit are fixed once created. The caller has checked
    /// that a newly chosen category is active (a product may stay in a category that was retired).
    /// </summary>
    public void Update(string nameAr, string? nameEn, Guid? categoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        NameAr = nameAr.Trim();
        NameEn = Text.Optional(nameEn);
        CategoryId = categoryId;
    }

    /// <summary>
    /// Adds a unit the product is also bought or sold in, holding <paramref name="conversionFactor"/> base units.
    /// When the base unit can't be split, the factor must be whole (half a piece can't be counted).
    /// </summary>
    public Result<ProductUnit> AddUnit(Guid unitId, decimal conversionFactor, bool baseUnitAllowsFraction)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(conversionFactor);
        if (!IsActive)
            return ProductDomainErrors.NotActive;
        if (_units.Any(x => x.UnitId == unitId))
            return ProductUnitDomainErrors.UnitAlreadyAdded;
        if (!baseUnitAllowsFraction && conversionFactor != decimal.Truncate(conversionFactor))
            return ProductUnitDomainErrors.FactorMustBeWhole;

        var added = ProductUnit.Create(this, unitId, conversionFactor);
        _units.Add(added);
        return added;
    }

    /// <summary>The base unit is what stock is kept in, so it stays active. The caller checks the unit itself is active.</summary>
    public Result SetUnitActive(ProductUnit productUnit, bool isActive)
    {
        if (productUnit.Product != this)
            throw new ArgumentException("The unit belongs to another product.", nameof(productUnit));
        if (!isActive && productUnit.UnitId == BaseUnitId)
            return ProductUnitDomainErrors.BaseUnitMustStayActive;
        if (isActive) productUnit.Activate();
        else productUnit.Deactivate();
        return Result.Success();
    }

    /// <summary>Only an active product unit of an active product gets new barcodes. The database keeps barcodes unique.</summary>
    public Result<ProductBarcode> AddBarcode(ProductUnit productUnit, string barcode, BarcodeType type)
    {
        if (productUnit.Product != this)
            throw new ArgumentException("The unit belongs to another product.", nameof(productUnit));
        if (!BarcodeFormat.IsValid(type, barcode))
            return ProductBarcodeDomainErrors.InvalidFormat;
        if (!IsActive)
            return ProductDomainErrors.NotActive;
        if (!productUnit.IsActive)
            return ProductUnitDomainErrors.NotActive;
        return productUnit.AddBarcode(barcode, type);
    }

    /// <summary>A barcode is reactivated only while its product unit is active.</summary>
    public Result SetBarcodeActive(ProductBarcode barcode, bool isActive)
    {
        if (barcode.ProductUnit.Product != this)
            throw new ArgumentException("The barcode belongs to another product.", nameof(barcode));
        if (isActive && !barcode.ProductUnit.IsActive)
            return ProductUnitDomainErrors.NotActive;
        if (isActive) barcode.Activate();
        else barcode.Deactivate();
        return Result.Success();
    }
}
