using FluentValidation;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.UpdatePurchaseOrder;

public sealed class UpdatePurchaseOrderValidator : AbstractValidator<UpdatePurchaseOrderCommand>
{
    public UpdatePurchaseOrderValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.SupplierReference).MaximumLength(100);
        RuleFor(x => x.PaymentTermDays).InclusiveBetween(0, 365);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty()
            .Must(l => l.Count <= PurchaseOrder.MaxLines).WithMessage("An order holds at most 200 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(OrderLines.Rules);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
