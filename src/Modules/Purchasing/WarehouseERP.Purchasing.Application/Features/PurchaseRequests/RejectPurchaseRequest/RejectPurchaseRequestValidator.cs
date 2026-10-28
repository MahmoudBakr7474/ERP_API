using FluentValidation;
using WarehouseERP.Purchasing.Application.Shared;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.RejectPurchaseRequest;

public sealed class RejectPurchaseRequestValidator : AbstractValidator<RejectPurchaseRequestCommand>
{
    public RejectPurchaseRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
