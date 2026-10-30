using FluentValidation;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetIncomeStatement;

public sealed class GetIncomeStatementValidator : AbstractValidator<GetIncomeStatementQuery>
{
    public GetIncomeStatementValidator()
    {
        RuleFor(x => x.FromDate).GreaterThanOrEqualTo(new DateOnly(2000, 1, 1));
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
        RuleFor(x => x.BranchId).NotEqual(Guid.Empty).When(x => x.BranchId.HasValue);
        RuleFor(x => x.CompareToDate).NotNull().When(x => x.CompareFromDate.HasValue)
            .WithMessage("A comparison period has both dates.");
        RuleFor(x => x.CompareFromDate).NotNull().When(x => x.CompareToDate.HasValue)
            .WithMessage("A comparison period has both dates.");
        RuleFor(x => x.CompareToDate).GreaterThanOrEqualTo(x => x.CompareFromDate)
            .When(x => x.CompareFromDate.HasValue && x.CompareToDate.HasValue);
    }
}
