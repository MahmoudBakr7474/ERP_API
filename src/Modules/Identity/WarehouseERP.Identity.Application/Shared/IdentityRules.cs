using FluentValidation;

namespace WarehouseERP.Identity.Application.Shared;

// Input shape shared by several areas. Password strength is ASP.NET Core Identity's policy (Identity:Password),
// reported as validation errors by IUserAccounts and IUserCredentials.
internal static class IdentityRules
{
    public static IRuleBuilderOptions<T, string> ValidUserName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(256).Matches("^[A-Za-z0-9._@-]+$");

    public static IRuleBuilderOptions<T, string> ValidDisplayName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(150);

    public static IRuleBuilderOptions<T, string?> ValidOptionalEmail<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(256).EmailAddress();

    public static IRuleBuilderOptions<T, string> PresentPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(256);

    public static IRuleBuilderOptions<T, string> ValidRoleName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(100);
}
