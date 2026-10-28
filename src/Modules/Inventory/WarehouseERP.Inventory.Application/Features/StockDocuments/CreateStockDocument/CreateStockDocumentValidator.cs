using FluentValidation;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.CreateStockDocument;

public sealed class CreateStockDocumentValidator : AbstractValidator<CreateStockDocumentCommand>
{
    public CreateStockDocumentValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.DocumentType).IsInEnum()
            .NotEqual(StockDocumentType.Reversal).WithMessage("A reversal is created from the posted document it reverses.");
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.AdjustmentReason).IsInEnum().When(x => x.AdjustmentReason.HasValue);
        RuleFor(x => x.ExternalReference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty()
            .Must(l => l.Count <= StockDocument.MaxLines).WithMessage("A document holds at most 500 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(StockLineRules.Apply);
    }
}
