namespace WarehouseERP.Catalog.Application.Features.ProductBarcodes.SetProductBarcodeActive;

public sealed record SetProductBarcodeActiveCommand(Guid ProductId, Guid BarcodeId, bool IsActive);
