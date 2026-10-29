using FluentValidation;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Application.Features.Deliveries.CreateSalesDelivery;

public sealed class CreateSalesDeliveryValidator : AbstractValidator<CreateSalesDeliveryCommand>
{
    public CreateSalesDeliveryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty().Must(l => l.Count <= SalesDelivery.MaxLines).WithMessage("A delivery holds at most 500 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(DeliveryLines.Rules);
    }
}
