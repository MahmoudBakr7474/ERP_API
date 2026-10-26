using FluentValidation;

namespace WarehouseERP.Catalog.Application.Features.Products.UpdateProduct;

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(250);
        RuleFor(x => x.NameEn).MaximumLength(250);
        RuleFor(x => x.RowVersion).NotEmpty()
            .Must(v => RowVersions.TryParse(v, out _))
            .WithErrorCode("RowVersionValidator")
            .WithMessage("'Row Version' must be the value read with the product.");
    }
}

internal static class RowVersions
{
    // SQL Server rowversion: 8 bytes, sent to clients as base64.
    public static bool TryParse(string? value, out byte[] rowVersion)
    {
        rowVersion = [];
        if (string.IsNullOrEmpty(value)) return false;
        var buffer = new byte[8];
        if (!Convert.TryFromBase64String(value, buffer, out var written) || written != 8) return false;
        rowVersion = buffer;
        return true;
    }
}
