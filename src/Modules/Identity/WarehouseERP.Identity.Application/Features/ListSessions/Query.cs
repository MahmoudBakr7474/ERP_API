namespace WarehouseERP.Identity.Application.Features.ListSessions;

/// <param name="UserId">Whose sessions; null for the caller's own. Another user's need identity.sessions.manage.</param>
public sealed record ListSessionsQuery(Guid? UserId);
