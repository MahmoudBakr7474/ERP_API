using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.CancelStockDocument;

public sealed class CancelStockDocumentValidator : AbstractValidator<CancelStockDocumentCommand>
{
    public CancelStockDocumentValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
