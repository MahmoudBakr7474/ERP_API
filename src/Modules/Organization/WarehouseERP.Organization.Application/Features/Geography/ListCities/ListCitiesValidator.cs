using FluentValidation;

namespace WarehouseERP.Organization.Application.Features.Geography.ListCities;

public sealed class ListCitiesValidator : AbstractValidator<ListCitiesQuery>
{
    public ListCitiesValidator()
    {
        RuleFor(x => x.GovernorateId).NotEmpty();
    }
}
