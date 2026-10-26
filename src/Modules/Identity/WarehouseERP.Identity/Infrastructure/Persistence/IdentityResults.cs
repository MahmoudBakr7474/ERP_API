using Microsoft.AspNetCore.Identity;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Infrastructure.Persistence;

// ASP.NET Core Identity's failures as the module's errors: a taken user name is a conflict, policy failures are
// field validation errors carrying Identity's codes (PasswordTooShort, PasswordRequiresDigit, ...).
internal static class IdentityResults
{
    public static Result ToResult(IdentityResult result) => result.Succeeded ? Result.Success() : ToError(result, "");

    public static Error ToError(IdentityResult result, string passwordProperty)
    {
        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateUserName)))
            return UserErrors.UserNameTaken;
        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure)))
            return CommonErrors.ConcurrencyConflict;
        return Error.Validation(result.Errors
            .Select(e => new ValidationError(PropertyFor(e.Code, passwordProperty), e.Code, e.Description))
            .ToList());
    }

    private static string PropertyFor(string code, string passwordProperty) =>
        code.StartsWith("Password", StringComparison.Ordinal) ? passwordProperty
        : code.Contains("UserName", StringComparison.Ordinal) ? "UserName"
        : code.Contains("Email", StringComparison.Ordinal) ? "Email"
        : "";
}
