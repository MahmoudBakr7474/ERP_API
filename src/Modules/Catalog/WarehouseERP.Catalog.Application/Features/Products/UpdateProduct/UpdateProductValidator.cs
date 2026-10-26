using FluentValidation;

namespace WarehouseERP.Catalog.Application.Features.Products.UpdateProduct;

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(250);
        RuleFor(x => x.NameEn).MaximumLength(250);
        RuleFor(x => x.RowVersion).NotEmpty()
            .Must(v => RowVersions.TryParse(v, out _))
            .WithErrorCode("RowVersionValidator")
            .WithMessage("'Row Version' must be the value read with the product.");
    }
}
