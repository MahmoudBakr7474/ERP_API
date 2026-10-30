using FluentValidation;
using WarehouseERP.Accounting.Application.Shared;

namespace WarehouseERP.Accounting.Application.Features.Vouchers.UpdateVoucher;

public sealed class UpdateVoucherValidator : AbstractValidator<UpdateVoucherCommand>
{
    public UpdateVoucherValidator()
    {
        RuleFor(x => x.PartyType).IsInEnum();
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.WithholdingAmount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0m);
        this.AddVoucherRules(x => x.Description, x => x.Reference, x => x.Lines, x => x.Allocations);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
