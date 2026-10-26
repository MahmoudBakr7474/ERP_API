using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Application.Contracts.Sessions;

public sealed record SessionResponse(
    Guid Id, SessionClientType ClientType, string? DeviceId, DateTime CreatedAtUtc, DateTime LastSeenAtUtc, DateTime ExpiresAtUtc,
    DateTime? RevokedAtUtc, SessionRevokeReason? RevokedReason);
