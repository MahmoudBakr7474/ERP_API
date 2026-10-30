using FluentValidation;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetBalanceSheet;

public sealed class GetBalanceSheetValidator : AbstractValidator<GetBalanceSheetQuery>
{
    public GetBalanceSheetValidator() => RuleFor(x => x.AsOfDate).GreaterThanOrEqualTo(new DateOnly(2000, 1, 1));
}
