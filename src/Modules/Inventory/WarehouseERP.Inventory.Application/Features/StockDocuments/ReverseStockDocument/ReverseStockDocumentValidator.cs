using FluentValidation;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.ReverseStockDocument;

public sealed class ReverseStockDocumentValidator : AbstractValidator<ReverseStockDocumentCommand>
{
    public ReverseStockDocumentValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
