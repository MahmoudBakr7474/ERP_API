using FluentValidation;

namespace WarehouseERP.Accounting.Application.Features.Allocations.AutoAllocate;

public sealed class AutoAllocateValidator : AbstractValidator<AutoAllocateCommand>
{
    public AutoAllocateValidator()
    {
        RuleFor(x => x.PartyType).IsInEnum();
        RuleFor(x => x.PartyId).NotEmpty();
    }
}
