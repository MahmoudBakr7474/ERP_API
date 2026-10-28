using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.SubmitStockDocument;

public sealed class SubmitStockDocumentValidator : AbstractValidator<SubmitStockDocumentCommand>
{
    public SubmitStockDocumentValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
