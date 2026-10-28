using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Application.Features.Transfers.UpdateStockTransfer;

public sealed class UpdateStockTransferValidator : AbstractValidator<UpdateStockTransferCommand>
{
    public UpdateStockTransferValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.TransferId).NotEmpty();
        RuleFor(x => x.TransitWarehouseId).NotEqual(Guid.Empty).When(x => x.TransitWarehouseId.HasValue);
        RuleFor(x => x.ExternalReference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty()
            .Must(l => l.Count <= StockTransfer.MaxLines).WithMessage("A transfer holds at most 500 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(TransferLineRules.Apply);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
