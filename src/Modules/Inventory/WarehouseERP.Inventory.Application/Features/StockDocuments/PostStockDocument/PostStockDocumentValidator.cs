using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.PostStockDocument;

public sealed class PostStockDocumentValidator : AbstractValidator<PostStockDocumentCommand>
{
    public PostStockDocumentValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Send an Idempotency-Key header (a new GUID per action, the same on a retry).");
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
