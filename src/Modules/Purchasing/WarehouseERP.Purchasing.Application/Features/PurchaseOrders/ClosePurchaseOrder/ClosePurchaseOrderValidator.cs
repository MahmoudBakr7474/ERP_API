using FluentValidation;
using WarehouseERP.Purchasing.Application.Shared;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.ClosePurchaseOrder;

public sealed class ClosePurchaseOrderValidator : AbstractValidator<ClosePurchaseOrderCommand>
{
    public ClosePurchaseOrderValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
