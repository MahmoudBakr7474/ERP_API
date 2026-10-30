using FluentValidation;

namespace WarehouseERP.Organization.Application.Features.Exports.ExportLocations;

public sealed class ExportLocationsValidator : AbstractValidator<ExportLocationsQuery>
{
    public ExportLocationsValidator()
    {
        RuleFor(x => x.MaxRows).GreaterThan(0);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.BranchId).NotEqual(Guid.Empty).When(x => x.BranchId.HasValue);
        RuleFor(x => x.WarehouseId).NotEqual(Guid.Empty).When(x => x.WarehouseId.HasValue);
        RuleFor(x => x.WarehouseType).IsInEnum().When(x => x.WarehouseType.HasValue);
        RuleFor(x => x.Sort).IsInEnum().When(x => x.Sort.HasValue);
    }
}
