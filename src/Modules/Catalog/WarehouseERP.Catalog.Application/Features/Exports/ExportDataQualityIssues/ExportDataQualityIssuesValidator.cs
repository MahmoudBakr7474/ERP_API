using FluentValidation;

namespace WarehouseERP.Catalog.Application.Features.Exports.ExportDataQualityIssues;

public sealed class ExportDataQualityIssuesValidator : AbstractValidator<ExportDataQualityIssuesQuery>
{
    public ExportDataQualityIssuesValidator()
    {
        RuleFor(x => x.MaxRows).GreaterThan(0);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Issue).IsInEnum().When(x => x.Issue.HasValue);
    }
}
