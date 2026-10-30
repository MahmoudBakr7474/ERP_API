using FluentValidation;

namespace WarehouseERP.Organization.Application.Features.Exports.ExportStructure;

public sealed class ExportStructureValidator : AbstractValidator<ExportStructureQuery>
{
    public ExportStructureValidator() => RuleFor(x => x.MaxRows).GreaterThan(0);
}
