using WarehouseERP.Catalog.Contracts;


namespace WarehouseERP.Catalog.Application.Features.ProductBarcodes.AddProductBarcode;

/// <param name="Barcode">Exactly as printed. EAN-13 and GTIN values must carry a valid GS1 check digit.</param>
public sealed record AddProductBarcodeCommand(Guid ProductId, Guid ProductUnitId, string Barcode, BarcodeType BarcodeType);
