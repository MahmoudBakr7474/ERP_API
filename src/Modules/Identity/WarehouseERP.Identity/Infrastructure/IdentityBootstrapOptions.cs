namespace WarehouseERP.Identity.Infrastructure;

/// <summary>
/// Bound to <c>Identity:Bootstrap</c>: the first administrator, created at startup only while no user exists.
/// Set it from the environment or a secret store, never appsettings, and remove it once the account exists.
/// </summary>
public sealed class IdentityBootstrapOptions
{
    public const string SectionName = "Identity:Bootstrap";

    public string? UserName { get; init; }
    public string? Password { get; init; }
    public string DisplayName { get; init; } = "Administrator";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrEmpty(Password);
}
