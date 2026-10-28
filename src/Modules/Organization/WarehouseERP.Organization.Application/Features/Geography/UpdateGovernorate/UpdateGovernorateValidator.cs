using FluentValidation;
using WarehouseERP.Organization.Application.Shared;

namespace WarehouseERP.Organization.Application.Features.Geography.UpdateGovernorate;

public sealed class UpdateGovernorateValidator : AbstractValidator<UpdateGovernorateCommand>
{
    public UpdateGovernorateValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.GovernorateId).NotEmpty();
        RuleFor(x => x.NameAr).MustBeArabicName();
        RuleFor(x => x.NameEn).MustBeEnglishName();
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
