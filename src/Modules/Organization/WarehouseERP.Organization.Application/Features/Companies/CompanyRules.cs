using FluentValidation;

namespace WarehouseERP.Organization.Application.Features.Companies;

// Input rules the company setup and the settings update share.
internal static class CompanyRules
{
    // An IANA id (Africa/Cairo) or a Windows one; the host's time zone database decides.
    public static IRuleBuilderOptions<T, string> MustBeTimeZone<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MaximumLength(100)
            .Must(id => TimeZoneInfo.TryFindSystemTimeZoneById(id.Trim(), out _))
            .WithErrorCode("TimeZoneValidator")
            .WithMessage("'Time Zone Id' must be a known time zone, such as Africa/Cairo.");

    // varchar(50) column: printable ASCII only, so nothing is lost on save.
    public static IRuleBuilderOptions<T, string?> MustBeTaxRegistrationNumber<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(50).Matches("^[ -~]*$");

    // varchar(30): the register number as issued: ASCII letters, digits, spaces, / and -.
    public static IRuleBuilderOptions<T, string?> MustBeCommercialRegistrationNumber<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(30).Matches("^[A-Za-z0-9 /-]*$");

    public static IRuleBuilderOptions<T, string?> MustBePhone<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(30).Matches("^[0-9+() -]*$");

    public static IRuleBuilderOptions<T, string?> MustBeEmail<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(100).EmailAddress();
}
