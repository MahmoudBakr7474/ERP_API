using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Settings.UpdateSalesSettings;

public sealed class UpdateSalesSettingsValidator : AbstractValidator<UpdateSalesSettingsCommand>
{
    public UpdateSalesSettingsValidator()
    {
        RuleFor(x => x.DiscountApprovalThreshold!.Value).GreaterThanOrEqualTo(0).LessThan(100)
            .OverridePropertyName("DiscountApprovalThreshold").When(x => x.DiscountApprovalThreshold.HasValue);
        RuleFor(x => x.CreditHoldPolicy).IsInEnum();
        RuleFor(x => x.DefaultVatRate).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.QuotationValidityDays).InclusiveBetween(1, 365);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
