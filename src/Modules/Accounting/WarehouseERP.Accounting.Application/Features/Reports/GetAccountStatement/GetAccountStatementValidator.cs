using FluentValidation;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetAccountStatement;

public sealed class GetAccountStatementValidator : AbstractValidator<GetAccountStatementQuery>
{
    public GetAccountStatementValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.FromDate).GreaterThanOrEqualTo(new DateOnly(2000, 1, 1));
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
        RuleFor(x => x.BranchId).NotEqual(Guid.Empty).When(x => x.BranchId.HasValue);
    }
}
