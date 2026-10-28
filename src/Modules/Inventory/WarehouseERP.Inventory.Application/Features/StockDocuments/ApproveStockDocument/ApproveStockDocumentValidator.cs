using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.ApproveStockDocument;

public sealed class ApproveStockDocumentValidator : AbstractValidator<ApproveStockDocumentCommand>
{
    public ApproveStockDocumentValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Send an Idempotency-Key header (a new GUID per action, the same on a retry).");
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
