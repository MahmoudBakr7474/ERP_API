using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;

/// <summary>
/// User accounts: finding, creating, profile and activation (ASP.NET Core Identity behind it). Changes are tracked
/// only; the use case commits them through <see cref="Persistence.IIdentityPersistence"/>.
/// </summary>
public interface IUserAccounts
{
    Task<UserAccount?> FindAsync(Guid userId, CancellationToken ct);

    /// <summary>Creates the account; password policy failures come back as validation errors.</summary>
    Task<Result<Guid>> CreateAsync(NewUserAccount account, CancellationToken ct);

    Task<Result> UpdateProfileAsync(Guid userId, string displayName, string? email, CancellationToken ct);

    Task<Result> SetActiveAsync(Guid userId, bool isActive, CancellationToken ct);
}
