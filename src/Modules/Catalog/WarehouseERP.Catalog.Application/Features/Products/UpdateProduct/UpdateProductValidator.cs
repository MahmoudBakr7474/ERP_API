using FluentValidation;
using WarehouseERP.Catalog.Application.Shared;

namespace WarehouseERP.Catalog.Application.Features.Products.UpdateProduct;

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(250);
        RuleFor(x => x.NameEn).MaximumLength(250);
        RuleFor(x => x.CategoryId).NotEqual(Guid.Empty).When(x => x.CategoryId.HasValue);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
