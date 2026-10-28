using FluentValidation;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.CreateSupplierReturn;

public sealed class CreateSupplierReturnValidator : AbstractValidator<CreateSupplierReturnCommand>
{
    public CreateSupplierReturnValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.GoodsReceiptId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty()
            .Must(l => l.Count <= SupplierReturn.MaxLines).WithMessage("A return holds at most 200 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(ReturnLines.Rules);
    }
}
