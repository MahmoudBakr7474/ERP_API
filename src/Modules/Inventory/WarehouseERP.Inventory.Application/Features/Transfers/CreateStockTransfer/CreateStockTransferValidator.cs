using FluentValidation;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Application.Features.Transfers.CreateStockTransfer;

public sealed class CreateStockTransferValidator : AbstractValidator<CreateStockTransferCommand>
{
    public CreateStockTransferValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.SourceWarehouseId).NotEmpty();
        RuleFor(x => x.DestinationWarehouseId).NotEmpty();
        RuleFor(x => x.TransitWarehouseId).NotEqual(Guid.Empty).When(x => x.TransitWarehouseId.HasValue);
        RuleFor(x => x.ExternalReference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty()
            .Must(l => l.Count <= StockTransfer.MaxLines).WithMessage("A transfer holds at most 500 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(TransferLineRules.Apply);
    }
}
