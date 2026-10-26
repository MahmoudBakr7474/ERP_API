using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;

/// <summary>
/// Passwords and lockout (ASP.NET Core Identity behind it). A wrong password counts toward lockout; the count is
/// persisted at the use case's save point.
/// </summary>
public interface IUserCredentials
{
    Task<LoginCheck> CheckPasswordAsync(string userName, string password, CancellationToken ct);

    Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct);

    /// <summary>An administrator sets a new password; it also lifts a lockout.</summary>
    Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct);

    Task<Result> UnlockAsync(Guid userId, CancellationToken ct);
}
