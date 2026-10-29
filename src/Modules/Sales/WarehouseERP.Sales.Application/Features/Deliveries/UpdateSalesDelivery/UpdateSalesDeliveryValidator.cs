using FluentValidation;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Application.Features.Deliveries.UpdateSalesDelivery;

public sealed class UpdateSalesDeliveryValidator : AbstractValidator<UpdateSalesDeliveryCommand>
{
    public UpdateSalesDeliveryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty().Must(l => l.Count <= SalesDelivery.MaxLines).WithMessage("A delivery holds at most 500 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(DeliveryLines.Rules);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
