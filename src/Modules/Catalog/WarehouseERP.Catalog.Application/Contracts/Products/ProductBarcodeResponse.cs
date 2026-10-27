using WarehouseERP.Catalog.Contracts;


namespace WarehouseERP.Catalog.Application.Contracts.Products;

public sealed record ProductBarcodeResponse(Guid Id, string Barcode, BarcodeType BarcodeType, bool IsActive);
