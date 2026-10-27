using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Errors;

public static class ProductBarcodeErrors
{
    // The unique index covers retired barcodes too, so a value is never reused.
    public static readonly Error Taken = Error.Conflict(
        "Catalog.Barcode.Taken", "This barcode is already used, or was used before and stays reserved.");

    public static readonly Error NotFound = Error.NotFound(
        "Catalog.Barcode.NotFound", "No such barcode.");

    public static readonly Error NoSellableItem = Error.NotFound(
        "Catalog.Barcode.NoSellableItem", "No active product unit has this active barcode.");
}
