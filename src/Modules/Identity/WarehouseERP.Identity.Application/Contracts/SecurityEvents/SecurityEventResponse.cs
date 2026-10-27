using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Application.Contracts.SecurityEvents;

/// <param name="ActorDisplayName">Null for anonymous actions or when the user no longer exists.</param>
/// <param name="Details">A small JSON object (ids, counts, outcome) or null.</param>
public sealed record SecurityEventResponse(
    long Id, DateTime OccurredAtUtc, SecurityEventType Type, Guid? ActorUserId, string? ActorDisplayName, Guid? TargetUserId,
    string? TargetDisplayName, Guid? RoleId, Guid? SessionId, string? IpAddress, string? Details);
