using FluentValidation;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Application.Features.ProductTaxes.SetProductTax;

public sealed class SetProductTaxValidator : AbstractValidator<SetProductTaxCommand>
{
    public SetProductTaxValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.TaxTreatment).IsInEnum();
        RuleFor(x => x.TaxRate!.Value).GreaterThan(0).LessThanOrEqualTo(100)
            .Must(p => Amounts.FitsScale(p, Amounts.PercentScale)).WithMessage("'Tax Rate' may have at most 4 decimals.")
            .OverridePropertyName("TaxRate").When(x => x.TaxRate.HasValue && x.TaxTreatment == SalesTaxTreatment.Standard);
        RuleFor(x => x.TaxRate).Null().WithMessage("A zero-rated or exempt product takes no tax rate.")
            .When(x => x.TaxTreatment != SalesTaxTreatment.Standard);
    }
}
