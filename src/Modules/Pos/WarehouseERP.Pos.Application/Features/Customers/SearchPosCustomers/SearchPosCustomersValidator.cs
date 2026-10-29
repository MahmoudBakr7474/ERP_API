using FluentValidation;

namespace WarehouseERP.Pos.Application.Features.Customers.SearchPosCustomers;

public sealed class SearchPosCustomersValidator : AbstractValidator<SearchPosCustomersQuery>
{
    public SearchPosCustomersValidator()
    {
        RuleFor(x => x.Search).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Take!.Value).InclusiveBetween(1, 50).OverridePropertyName("Take").When(x => x.Take.HasValue);
    }
}
