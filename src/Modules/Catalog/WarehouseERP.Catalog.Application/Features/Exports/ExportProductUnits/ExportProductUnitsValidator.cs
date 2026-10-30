using FluentValidation;

namespace WarehouseERP.Catalog.Application.Features.Exports.ExportProductUnits;

public sealed class ExportProductUnitsValidator : AbstractValidator<ExportProductUnitsQuery>
{
    public ExportProductUnitsValidator()
    {
        RuleFor(x => x.MaxRows).GreaterThan(0);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.CategoryId).NotEqual(Guid.Empty).When(x => x.CategoryId.HasValue);
        RuleFor(x => x.UnitId).NotEqual(Guid.Empty).When(x => x.UnitId.HasValue);
        RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
    }
}
