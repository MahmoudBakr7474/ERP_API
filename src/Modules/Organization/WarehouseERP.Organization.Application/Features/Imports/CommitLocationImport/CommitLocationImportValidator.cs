using FluentValidation;

namespace WarehouseERP.Organization.Application.Features.Imports.CommitLocationImport;

public sealed class CommitLocationImportValidator : AbstractValidator<CommitLocationImportCommand>
{
    public CommitLocationImportValidator()
    {
        RuleFor(x => x.Mode).IsInEnum();
        RuleFor(x => x.FileFingerprint).NotEmpty().MaximumLength(128);
        RuleFor(x => x.PreviewToken).NotEmpty().MaximumLength(64);
    }
}
