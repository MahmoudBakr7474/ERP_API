using FluentValidation;

namespace WarehouseERP.Pos.Application.Features.Items.SearchPosItems;

public sealed class SearchPosItemsValidator : AbstractValidator<SearchPosItemsQuery>
{
    public SearchPosItemsValidator()
    {
        RuleFor(x => x.TerminalId).NotEmpty();
        RuleFor(x => x.Search).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Take!.Value).InclusiveBetween(1, 50).OverridePropertyName("Take").When(x => x.Take.HasValue);
    }
}
