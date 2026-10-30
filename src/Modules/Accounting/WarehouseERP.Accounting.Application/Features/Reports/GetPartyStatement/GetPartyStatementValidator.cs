using FluentValidation;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetPartyStatement;

public sealed class GetPartyStatementValidator : AbstractValidator<GetPartyStatementQuery>
{
    public GetPartyStatementValidator()
    {
        RuleFor(x => x.PartyType).IsInEnum();
        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.FromDate).GreaterThanOrEqualTo(new DateOnly(2000, 1, 1));
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
    }
}
