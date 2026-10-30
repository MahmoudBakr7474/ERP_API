using FluentValidation;

namespace WarehouseERP.Catalog.Application.Features.Exports.ExportBarcodes;

public sealed class ExportBarcodesValidator : AbstractValidator<ExportBarcodesQuery>
{
    public ExportBarcodesValidator()
    {
        RuleFor(x => x.MaxRows).GreaterThan(0);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.CategoryId).NotEqual(Guid.Empty).When(x => x.CategoryId.HasValue);
        RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
        RuleFor(x => x.BarcodeType).IsInEnum().When(x => x.BarcodeType.HasValue);
    }
}
