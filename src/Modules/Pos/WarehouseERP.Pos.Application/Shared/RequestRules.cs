using FluentValidation;
using WarehouseERP.Pos.Application.Contracts.Sales;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Application.Shared;

// The shape of cart lines and payment lines, shared by quotes, checkouts and returns. Business rules (prices, discount
// limits, what payments must add up to) are checked by the handlers and the domain.
internal static class RequestRules
{
    public static void CartLine(InlineValidator<PosCartLineRequest> line)
    {
        line.RuleLevelCascadeMode = CascadeMode.Stop;
        line.RuleFor(x => x.ProductId).NotEmpty();
        line.RuleFor(x => x.ProductUnitId).NotEmpty();
        line.RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(Money.MaxQuantity)
            .Must(q => Money.FitsScale(q, Money.QuantityScale)).WithMessage("'Quantity' may have at most 4 decimals.");
        line.RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100)
            .Must(p => Money.FitsScale(p, Money.PercentScale)).WithMessage("'Discount Percent' may have at most 4 decimals.");
    }

    public static void Payment(InlineValidator<PosPaymentRequest> payment)
    {
        payment.RuleLevelCascadeMode = CascadeMode.Stop;
        payment.RuleFor(x => x.Method).IsInEnum();
        payment.RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(Money.MaxAmount)
            .Must(a => Money.FitsScale(a, Money.AmountScale)).WithMessage("'Amount' may have at most 4 decimals.");
        payment.RuleFor(x => x.Reference).NotEmpty()
            .WithMessage("Enter the card terminal's authorization or reference number.").When(x => x.Method == PosPaymentMethod.Card);
        payment.RuleFor(x => x.Reference).MaximumLength(PosSale.MaxReferenceLength);
        payment.RuleFor(x => x.CardLast4).Matches("^[0-9]{4}$")
            .WithMessage("Only the card's last four digits may be recorded.").When(x => x.CardLast4 is not null);
    }
}
