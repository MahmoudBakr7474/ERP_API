namespace WarehouseERP.Identity.Domain;

/// <summary>
/// One entry of the security audit trail: who did what to whom, when and from where. Append-only: written in the
/// same transaction as the change it describes, never updated or deleted by the application. The one exception is a
/// refused sign-in or password change: its failed-attempt count or lockout is committed first, on its own, so it
/// counts even if writing the event then fails. It holds no secret (no password, token or typed user name) and no
/// foreign key, so it outlives the users and sessions it names.
/// </summary>
public sealed class SecurityEvent
{
    private SecurityEvent() { } // EF Core

    public long Id { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public SecurityEventType Type { get; private set; }

    /// <summary>The signed-in user who acted; null for anonymous actions (sign-in) and the bootstrap.</summary>
    public Guid? ActorUserId { get; private set; }

    /// <summary>The user the event is about (the one signing in, the one whose roles changed, ...).</summary>
    public Guid? TargetUserId { get; private set; }

    public Guid? RoleId { get; private set; }
    public Guid? SessionId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? CorrelationId { get; private set; }

    /// <summary>A small JSON object with what changed (ids, counts, outcome); never secrets.</summary>
    public string? Details { get; private set; }

    public static SecurityEvent Record(
        SecurityEventType type, DateTime occurredAtUtc, Guid? actorUserId, Guid? targetUserId, Guid? roleId, Guid? sessionId,
        string? ipAddress, string? correlationId, string? details) => new()
        {
            Type = type,
            OccurredAtUtc = occurredAtUtc,
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            RoleId = roleId,
            SessionId = sessionId,
            IpAddress = ipAddress,
            CorrelationId = correlationId,
            Details = details,
        };
}
