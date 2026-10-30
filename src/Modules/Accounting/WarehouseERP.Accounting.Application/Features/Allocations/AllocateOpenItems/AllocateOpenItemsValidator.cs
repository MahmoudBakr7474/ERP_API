using FluentValidation;

namespace WarehouseERP.Accounting.Application.Features.Allocations.AllocateOpenItems;

public sealed class AllocateOpenItemsValidator : AbstractValidator<AllocateOpenItemsCommand>
{
    public AllocateOpenItemsValidator()
    {
        RuleFor(x => x.DebitItemId).NotEmpty();
        RuleFor(x => x.CreditItemId).NotEmpty().NotEqual(x => x.DebitItemId);
        RuleFor(x => x.Amount).GreaterThan(0m);
    }
}
