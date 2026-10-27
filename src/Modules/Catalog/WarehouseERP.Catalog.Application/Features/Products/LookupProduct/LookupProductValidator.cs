using FluentValidation;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Application.Features.Products.LookupProduct;

public sealed class LookupProductValidator : AbstractValidator<LookupProductQuery>
{
    public LookupProductValidator() =>
        RuleFor(x => x.Barcode).NotEmpty().MaximumLength(BarcodeFormat.MaxLength);
}
