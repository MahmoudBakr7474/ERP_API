using FluentValidation;
using WarehouseERP.Purchasing.Application.Shared;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.CancelPurchaseOrder;

public sealed class CancelPurchaseOrderValidator : AbstractValidator<CancelPurchaseOrderCommand>
{
    public CancelPurchaseOrderValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
