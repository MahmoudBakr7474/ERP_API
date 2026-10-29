using FluentValidation;

namespace WarehouseERP.Pos.Application.Features.Items.LookupPosItem;

public sealed class LookupPosItemValidator : AbstractValidator<LookupPosItemQuery>
{
    public LookupPosItemValidator()
    {
        RuleFor(x => x.TerminalId).NotEmpty();
        RuleFor(x => x.Barcode).NotEmpty().MaximumLength(50);
    }
}
