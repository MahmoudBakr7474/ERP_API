using FluentValidation;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Application.Features.Returns.UpdateSalesReturn;

public sealed class UpdateSalesReturnValidator : AbstractValidator<UpdateSalesReturnCommand>
{
    public UpdateSalesReturnValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty().Must(l => l.Count <= SalesReturn.MaxLines).WithMessage("A return holds at most 500 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(ReturnLines.Rules);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
