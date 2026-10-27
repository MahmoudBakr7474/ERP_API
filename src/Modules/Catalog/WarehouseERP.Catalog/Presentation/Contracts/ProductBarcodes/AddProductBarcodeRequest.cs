using WarehouseERP.Catalog.Contracts;


namespace WarehouseERP.Catalog.Presentation.Contracts.ProductBarcodes;

public sealed record AddProductBarcodeRequest(string Barcode, BarcodeType BarcodeType);
