namespace WarehouseERP.Identity.Application.Contracts.Authentication;

/// <summary>The server enforces these; a client shows them as hints and may pre-check, never relax them.</summary>
public sealed record PasswordPolicyResponse(
    int RequiredLength, int RequiredUniqueChars, bool RequireDigit, bool RequireLowercase, bool RequireUppercase,
    bool RequireNonAlphanumeric);
