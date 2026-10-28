using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Features.DocumentPolicies.UpdateDocumentPolicy;

public sealed class UpdateDocumentPolicyValidator : AbstractValidator<UpdateDocumentPolicyCommand>
{
    public UpdateDocumentPolicyValidator()
    {
        RuleFor(x => x.DocumentType).IsInEnum()
            .NotEqual(StockDocumentType.Reversal).WithMessage("A reversal always needs approval.");
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
