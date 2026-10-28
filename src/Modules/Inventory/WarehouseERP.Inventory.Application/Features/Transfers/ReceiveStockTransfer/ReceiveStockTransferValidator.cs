using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Application.Features.Transfers.ReceiveStockTransfer;

public sealed class ReceiveStockTransferValidator : AbstractValidator<ReceiveStockTransferCommand>
{
    public ReceiveStockTransferValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.TransferId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Send an Idempotency-Key header (a new GUID per action, the same on a retry).");
        RuleFor(x => x.Lines).NotEmpty()
            .Must(l => l.Count <= StockTransfer.MaxLines).WithMessage("A receipt holds at most 500 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(line =>
        {
            line.RuleLevelCascadeMode = CascadeMode.Stop;
            line.RuleFor(x => x.LineId).NotEmpty();
            line.RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .LessThanOrEqualTo(Quantities.MaxQuantity)
                .Must(q => Quantities.FitsScale(q, Quantities.QuantityScale))
                .WithMessage("'Quantity' may have at most 4 decimals.");
            line.RuleFor(x => x.LocationId).NotEqual(Guid.Empty).When(x => x.LocationId.HasValue);
        });
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
