using FluentValidation;

namespace WarehouseERP.Organization.Application.Features.Imports.PreviewLocationImport;

public sealed class PreviewLocationImportValidator : AbstractValidator<PreviewLocationImportQuery>
{
    public PreviewLocationImportValidator()
    {
        RuleFor(x => x.Mode).IsInEnum();
        RuleFor(x => x.FileFingerprint).NotEmpty().MaximumLength(128);
    }
}
