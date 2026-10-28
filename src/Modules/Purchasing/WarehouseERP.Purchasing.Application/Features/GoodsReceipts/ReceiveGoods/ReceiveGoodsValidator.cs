using FluentValidation;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Application.Features.GoodsReceipts.ReceiveGoods;

public sealed class ReceiveGoodsValidator : AbstractValidator<ReceiveGoodsCommand>
{
    public ReceiveGoodsValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Send an Idempotency-Key header: a new GUID per receipt, the same one on a retry.");
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.SupplierDeliveryNote).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty()
            .Must(l => l.Count <= PurchaseOrder.MaxLines).WithMessage("A receipt holds at most 200 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(line =>
        {
            line.RuleLevelCascadeMode = CascadeMode.Stop;
            line.RuleFor(x => x.PurchaseOrderLineId).NotEmpty();
            line.RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(Amounts.MaxQuantity)
                .Must(q => Amounts.FitsScale(q, Amounts.QuantityScale)).WithMessage("'Quantity' may have at most 4 decimals.");
            line.RuleFor(x => x.LocationId).NotEqual(Guid.Empty).When(x => x.LocationId.HasValue);
        });
    }
}
