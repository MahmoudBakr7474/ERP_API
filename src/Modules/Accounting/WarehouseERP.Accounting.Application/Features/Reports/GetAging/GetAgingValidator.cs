using FluentValidation;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetAging;

public sealed class GetAgingValidator : AbstractValidator<GetAgingQuery>
{
    public GetAgingValidator()
    {
        RuleFor(x => x.PartyType).IsInEnum();
        RuleFor(x => x.AsOfDate).GreaterThanOrEqualTo(new DateOnly(2000, 1, 1));
        RuleFor(x => x.BranchId).NotEqual(Guid.Empty).When(x => x.BranchId.HasValue);
    }
}
