using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.UpdateStockDocument;

public sealed class UpdateStockDocumentValidator : AbstractValidator<UpdateStockDocumentCommand>
{
    public UpdateStockDocumentValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.AdjustmentReason).IsInEnum().When(x => x.AdjustmentReason.HasValue);
        RuleFor(x => x.ExternalReference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty()
            .Must(l => l.Count <= StockDocument.MaxLines).WithMessage("A document holds at most 500 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(StockLineRules.Apply);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
