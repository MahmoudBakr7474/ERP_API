using FluentValidation;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Application.Features.Returns.CreatePosReturn;

public sealed class CreatePosReturnValidator : AbstractValidator<CreatePosReturnCommand>
{
    public CreatePosReturnValidator()
    {
        RuleFor(x => x.TerminalId).NotEmpty();
        RuleFor(x => x.SaleId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(PosReturn.MaxReasonLength);
        RuleFor(x => x.Notes).MaximumLength(PosSale.MaxNotesLength);
        RuleFor(x => x.Lines).NotEmpty().Must(l => l.Count <= PosSale.MaxLines)
            .WithMessage($"A return has at most {PosSale.MaxLines} lines.");
        RuleForEach(x => x.Lines).NotNull().ChildRules(line =>
        {
            line.RuleFor(l => l.SaleLineId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0).LessThanOrEqualTo(Money.MaxQuantity)
                .Must(q => Money.FitsScale(q, Money.QuantityScale)).WithMessage("'Quantity' may have at most 4 decimals.");
            line.RuleFor(l => l.Condition).IsInEnum();
        });
        RuleFor(x => x.Refunds).Must(r => r.Count <= PosSale.MaxPayments)
            .WithMessage($"A return has at most {PosSale.MaxPayments} refund lines.");
        RuleForEach(x => x.Refunds).NotNull().ChildRules(RequestRules.Payment);
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Send an Idempotency-Key header (a new GUID per return).");
    }
}
