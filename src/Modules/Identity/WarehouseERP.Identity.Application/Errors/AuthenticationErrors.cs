using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Errors;

// Signing in and proving who you are.
public static class AuthenticationErrors
{
    // One answer for an unknown user, a wrong password, a locked-out or an inactive account, so user names
    // can't be probed; the real reason is only logged.
    public static readonly Error LoginFailed = Error.Unauthorized(
        "Identity.Login.Failed", "Wrong user name or password, or the account is temporarily locked.");

    public static readonly Error PasswordIncorrect = Error.BusinessRule(
        "Identity.Password.Incorrect", "The current password is not correct.");
}
