using FluentValidation;
using WarehouseERP.Purchasing.Application.Shared;

namespace WarehouseERP.Purchasing.Application.Features.PurchasingPolicies.UpdatePurchasingPolicy;

public sealed class UpdatePurchasingPolicyValidator : AbstractValidator<UpdatePurchasingPolicyCommand>
{
    public UpdatePurchasingPolicyValidator()
    {
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
