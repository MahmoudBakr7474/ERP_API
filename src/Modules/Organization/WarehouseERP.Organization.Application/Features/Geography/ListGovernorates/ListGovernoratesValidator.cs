using FluentValidation;

namespace WarehouseERP.Organization.Application.Features.Geography.ListGovernorates;

public sealed class ListGovernoratesValidator : AbstractValidator<ListGovernoratesQuery>
{
    public ListGovernoratesValidator()
    {
        RuleFor(x => x.CountryCode).NotEmpty().Matches(GeographyRules.CountryCodePattern);
    }
}
