using FluentValidation;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Application.Features.Returns.CreateSalesReturn;

public sealed class CreateSalesReturnValidator : AbstractValidator<CreateSalesReturnCommand>
{
    public CreateSalesReturnValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.DeliveryId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty().Must(l => l.Count <= SalesReturn.MaxLines).WithMessage("A return holds at most 500 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(ReturnLines.Rules);
    }
}
