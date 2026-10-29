using FluentValidation;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Application.Features.Prices.SetSalesPrice;

public sealed class SetSalesPriceValidator : AbstractValidator<SetSalesPriceCommand>
{
    public SetSalesPriceValidator()
    {
        RuleFor(x => x.ProductUnitId).NotEmpty();
        RuleFor(x => x.UnitPrice).GreaterThan(0).LessThanOrEqualTo(Amounts.MaxUnitPrice)
            .Must(p => Amounts.FitsScale(p, Amounts.PriceScale)).WithMessage("'Unit Price' may have at most 8 decimals.");
    }
}
