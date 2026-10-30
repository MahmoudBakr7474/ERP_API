using FluentValidation;

namespace WarehouseERP.Organization.Application.Features.Exports.ExportWarehouses;

public sealed class ExportWarehousesValidator : AbstractValidator<ExportWarehousesQuery>
{
    public ExportWarehousesValidator()
    {
        RuleFor(x => x.MaxRows).GreaterThan(0);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.BranchId).NotEqual(Guid.Empty).When(x => x.BranchId.HasValue);
        RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
        RuleFor(x => x.Sort).IsInEnum().When(x => x.Sort.HasValue);
    }
}
