using FluentValidation;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Application.Features.ProductBarcodes.AddProductBarcode;

public sealed class AddProductBarcodeValidator : AbstractValidator<AddProductBarcodeCommand>
{
    public AddProductBarcodeValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ProductUnitId).NotEmpty();
        RuleFor(x => x.BarcodeType).IsInEnum();
        RuleFor(x => x.Barcode).NotEmpty().MaximumLength(BarcodeFormat.MaxLength)
            .Must((command, value) => BarcodeFormat.IsValid(command.BarcodeType, value))
            .WithErrorCode("BarcodeFormatValidator")
            .WithMessage(command => command.BarcodeType switch
            {
                BarcodeType.Ean13 => "An EAN-13 barcode is 13 digits with a valid check digit.",
                BarcodeType.Gtin => "A GTIN is 8, 12, 13 or 14 digits with a valid check digit.",
                _ => "A barcode is printable ASCII characters without spaces.",
            });
    }
}
