namespace WarehouseERP.Catalog.Application.Features.Products.LookupProduct;

/// <param name="Barcode">The scanned value, exactly as printed.</param>
public sealed record LookupProductQuery(string Barcode);
