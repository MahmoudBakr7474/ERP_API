using FluentValidation;

namespace WarehouseERP.Catalog.Application.Features.Imports.CommitCatalogImport;

public sealed class CommitCatalogImportValidator : AbstractValidator<CommitCatalogImportCommand>
{
    public CommitCatalogImportValidator()
    {
        RuleFor(x => x.Mode).IsInEnum();
        RuleFor(x => x.FileFingerprint).NotEmpty().MaximumLength(128);
        RuleFor(x => x.PreviewToken).NotEmpty().MaximumLength(64);
    }
}
