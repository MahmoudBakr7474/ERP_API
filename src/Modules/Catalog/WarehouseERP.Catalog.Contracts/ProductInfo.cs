namespace WarehouseERP.Catalog.Contracts;

/// <summary>A product as other modules see it.</summary>
/// <param name="Id">Product id.</param>
/// <param name="Sku">Internal stock-keeping code, unique.</param>
/// <param name="NameAr">Arabic name.</param>
/// <param name="BaseUnitId">The unit stock is kept in (conversion factor 1).</param>
/// <param name="Type">Stock item, non-stock item or service.</param>
/// <param name="TrackingMode">None, batch or serial; only stock items are tracked.</param>
/// <param name="IsActive">False once retired.</param>
public sealed record ProductInfo(
    Guid Id, string Sku, string NameAr, Guid BaseUnitId, ProductType Type, TrackingMode TrackingMode, bool IsActive);
