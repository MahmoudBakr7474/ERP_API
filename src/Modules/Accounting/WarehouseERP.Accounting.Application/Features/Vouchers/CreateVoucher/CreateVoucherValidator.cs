using FluentValidation;

namespace WarehouseERP.Accounting.Application.Features.Vouchers.CreateVoucher;

public sealed class CreateVoucherValidator : AbstractValidator<CreateVoucherCommand>
{
    public CreateVoucherValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.PartyType).IsInEnum();
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.WithholdingAmount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0m);
        this.AddVoucherRules(x => x.Description, x => x.Reference, x => x.Lines, x => x.Allocations);
    }
}
