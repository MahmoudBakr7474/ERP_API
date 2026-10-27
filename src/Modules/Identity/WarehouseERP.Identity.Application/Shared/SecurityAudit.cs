using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Shared;

// An attempt to grant or manage more than the caller holds is refused and recorded: the refusal changes nothing,
// so the event is the only thing saved. Any other failure is returned as it is. Used by the Users and Roles areas.
internal static class SecurityAudit
{
    public static async Task<Error> RefuseAsync(
        Error error, ISecurityEventLog events, IIdentityPersistence persistence, CancellationToken ct,
        Guid? targetUserId = null, Guid? roleId = null)
    {
        if (error != PermissionErrors.ExceedsOwnPermissions && error != UserAccessErrors.ExceedsOwnScope)
            return error;
        events.Record(SecurityEventType.PrivilegeEscalationBlocked, targetUserId, roleId, details: new { reason = error.Code });
        _ = await persistence.SaveChangesAsync(ct);
        return error;
    }
}
