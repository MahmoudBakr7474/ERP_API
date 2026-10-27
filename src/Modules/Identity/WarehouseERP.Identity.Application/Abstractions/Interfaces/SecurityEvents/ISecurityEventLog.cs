using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;

/// <summary>
/// The security audit trail. An event is tracked, not written: it is saved with the use case's other changes by
/// <see cref="Persistence.IIdentityPersistence"/>, so it exists if and only if the change does. The actor, the client
/// IP, the time and the correlation id are filled in by the implementation.
/// </summary>
public interface ISecurityEventLog
{
    /// <param name="details">A small object serialized as JSON (ids, counts, outcome); never a secret.</param>
    void Record(SecurityEventType type, Guid? targetUserId = null, Guid? roleId = null, Guid? sessionId = null, object? details = null);
}
