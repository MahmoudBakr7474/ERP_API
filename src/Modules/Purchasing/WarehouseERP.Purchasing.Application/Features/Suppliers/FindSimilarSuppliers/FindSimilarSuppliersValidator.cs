using FluentValidation;

namespace WarehouseERP.Purchasing.Application.Features.Suppliers.FindSimilarSuppliers;

public sealed class FindSimilarSuppliersValidator : AbstractValidator<FindSimilarSuppliersQuery>
{
    public FindSimilarSuppliersValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).MaximumLength(200);
    }
}
