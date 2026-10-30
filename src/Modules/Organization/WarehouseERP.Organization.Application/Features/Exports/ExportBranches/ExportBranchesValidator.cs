using FluentValidation;

namespace WarehouseERP.Organization.Application.Features.Exports.ExportBranches;

public sealed class ExportBranchesValidator : AbstractValidator<ExportBranchesQuery>
{
    public ExportBranchesValidator()
    {
        RuleFor(x => x.MaxRows).GreaterThan(0);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Sort).IsInEnum().When(x => x.Sort.HasValue);
    }
}
