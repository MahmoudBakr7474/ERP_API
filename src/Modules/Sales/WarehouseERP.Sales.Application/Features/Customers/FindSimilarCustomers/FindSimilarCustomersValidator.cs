using FluentValidation;

namespace WarehouseERP.Sales.Application.Features.Customers.FindSimilarCustomers;

public sealed class FindSimilarCustomersValidator : AbstractValidator<FindSimilarCustomersQuery>
{
    public FindSimilarCustomersValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).MaximumLength(200);
        RuleFor(x => x.Mobile).MaximumLength(30);
    }
}
