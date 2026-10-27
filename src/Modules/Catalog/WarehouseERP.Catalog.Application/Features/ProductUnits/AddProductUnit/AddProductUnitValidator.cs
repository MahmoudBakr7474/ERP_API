using FluentValidation;

namespace WarehouseERP.Catalog.Application.Features.ProductUnits.AddProductUnit;

public sealed class AddProductUnitValidator : AbstractValidator<AddProductUnitCommand>
{
    public AddProductUnitValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        // decimal(18, 8) column: at most 10 whole digits and 8 decimals, so nothing is rounded on save.
        RuleFor(x => x.ConversionFactor).GreaterThan(0).PrecisionScale(18, 8, ignoreTrailingZeros: true);
    }
}
