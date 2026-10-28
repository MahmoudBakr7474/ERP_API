using FluentValidation;
using WarehouseERP.Purchasing.Application.Shared;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.RejectPurchaseOrder;

public sealed class RejectPurchaseOrderValidator : AbstractValidator<RejectPurchaseOrderCommand>
{
    public RejectPurchaseOrderValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
