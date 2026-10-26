using FluentValidation;

namespace WarehouseERP.Catalog.Application.Features.CreateProduct;

// Input shape only. "Only stock items are tracked" is a domain invariant (Product.Create) and the
// unique SKU is guarded again by the database index.
public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        // varchar(80) column: ASCII only, so nothing is lost on save.
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(80).Matches("^[A-Za-z0-9._/-]+$");
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(250);
        RuleFor(x => x.NameEn).MaximumLength(250);
        RuleFor(x => x.BaseUnitId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEqual(Guid.Empty).When(x => x.CategoryId.HasValue);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.TrackingMode).IsInEnum();
    }
}
