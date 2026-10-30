using FluentValidation;

namespace WarehouseERP.Catalog.Application.Features.Imports.PreviewCatalogImport;

public sealed class PreviewCatalogImportValidator : AbstractValidator<PreviewCatalogImportQuery>
{
    public PreviewCatalogImportValidator()
    {
        RuleFor(x => x.Mode).IsInEnum();
        RuleFor(x => x.FileFingerprint).NotEmpty().MaximumLength(128);
    }
}
