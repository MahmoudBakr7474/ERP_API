using FluentValidation;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.UpdateSupplierReturn;

public sealed class UpdateSupplierReturnValidator : AbstractValidator<UpdateSupplierReturnCommand>
{
    public UpdateSupplierReturnValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty()
            .Must(l => l.Count <= SupplierReturn.MaxLines).WithMessage("A return holds at most 200 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(ReturnLines.Rules);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
