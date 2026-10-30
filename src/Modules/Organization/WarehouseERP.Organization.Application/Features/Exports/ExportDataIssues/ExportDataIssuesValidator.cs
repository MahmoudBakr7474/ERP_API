using FluentValidation;

namespace WarehouseERP.Organization.Application.Features.Exports.ExportDataIssues;

public sealed class ExportDataIssuesValidator : AbstractValidator<ExportDataIssuesQuery>
{
    public ExportDataIssuesValidator()
    {
        RuleFor(x => x.MaxRows).GreaterThan(0);
        RuleFor(x => x.Issue).IsInEnum().When(x => x.Issue.HasValue);
    }
}
