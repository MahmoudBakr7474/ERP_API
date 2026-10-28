using FluentValidation;
using WarehouseERP.Purchasing.Application.Shared;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.ClosePurchaseRequest;

public sealed class ClosePurchaseRequestValidator : AbstractValidator<ClosePurchaseRequestCommand>
{
    public ClosePurchaseRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
