using FluentValidation;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Application.Features.Sales.QuotePosCart;

public sealed class QuotePosCartValidator : AbstractValidator<QuotePosCartQuery>
{
    public QuotePosCartValidator()
    {
        RuleFor(x => x.TerminalId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().Must(l => l.Count <= PosSale.MaxLines)
            .WithMessage($"A sale has at most {PosSale.MaxLines} lines.");
        RuleForEach(x => x.Lines).NotNull().ChildRules(RequestRules.CartLine);
    }
}
