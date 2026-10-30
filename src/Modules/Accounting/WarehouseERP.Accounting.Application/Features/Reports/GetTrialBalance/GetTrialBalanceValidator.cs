using FluentValidation;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetTrialBalance;

public sealed class GetTrialBalanceValidator : AbstractValidator<GetTrialBalanceQuery>
{
    public GetTrialBalanceValidator()
    {
        RuleFor(x => x.FromDate).GreaterThanOrEqualTo(new DateOnly(2000, 1, 1));
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
        RuleFor(x => x.BranchId).NotEqual(Guid.Empty).When(x => x.BranchId.HasValue);
        RuleFor(x => x.Level).InclusiveBetween(1, Account.MaxLevel).When(x => x.Level.HasValue);
    }
}
