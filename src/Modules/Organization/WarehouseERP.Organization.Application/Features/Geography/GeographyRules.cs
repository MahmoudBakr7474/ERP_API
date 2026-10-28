using FluentValidation;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Application.Features.Geography;

// Input rules shared by the country, governorate and city use cases.
internal static class GeographyRules
{
    /// <summary>ISO 3166 alpha-2: two ASCII letters (stored upper case in a char(2) column).</summary>
    public const string CountryCodePattern = "^[A-Za-z]{2}$";

    public static IRuleBuilderOptions<T, string> MustBeArabicName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(GeographyNames.MaxLength);

    public static IRuleBuilderOptions<T, string?> MustBeEnglishName<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(GeographyNames.MaxLength);
}
