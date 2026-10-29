using FluentValidation;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Application.Features.Sales.CheckoutPosSale;

public sealed class CheckoutPosSaleValidator : AbstractValidator<CheckoutPosSaleCommand>
{
    public CheckoutPosSaleValidator()
    {
        RuleFor(x => x.TerminalId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEqual(Guid.Empty).When(x => x.CustomerId.HasValue);
        RuleFor(x => x.CustomerName).MaximumLength(PosSale.MaxCustomerNameLength);
        RuleFor(x => x.CustomerPhone).MaximumLength(PosSale.MaxPhoneLength);
        RuleFor(x => x.Notes).MaximumLength(PosSale.MaxNotesLength);
        RuleFor(x => x.Lines).NotEmpty().Must(l => l.Count <= PosSale.MaxLines)
            .WithMessage($"A sale has at most {PosSale.MaxLines} lines.");
        RuleForEach(x => x.Lines).NotNull().ChildRules(RequestRules.CartLine);
        RuleFor(x => x.Payments).NotEmpty().Must(p => p.Count <= PosSale.MaxPayments)
            .WithMessage($"A sale has at most {PosSale.MaxPayments} payment lines.");
        RuleForEach(x => x.Payments).NotNull().ChildRules(RequestRules.Payment);
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Send an Idempotency-Key header (a new GUID per checkout).");
    }
}
