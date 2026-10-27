using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Domain;

public static class ProductBarcodeDomainErrors
{
    /// <summary>What Product.AddBarcode returns when a direct caller skipped the validator and sent a bad value.</summary>
    public static readonly Error InvalidFormat = Error.Validation(
        [new ValidationError("Barcode", "BarcodeFormatValidator", "The barcode does not match its type.")]);
}
