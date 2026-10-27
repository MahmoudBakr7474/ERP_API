using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Infrastructure.Persistence;

// Adds the event to the shared DbContext; the use case's single save writes it with the change it describes.
// Who (signed-in user), when (TimeProvider), from where (client IP; behind a proxy only correct with the host's
// forwarded-headers setting) and the request's correlation id are filled in here, never by handlers.
internal sealed class SecurityEventLog(DbContext db, ICurrentUser currentUser, IHttpContextAccessor http, TimeProvider clock)
    : ISecurityEventLog
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public void Record(SecurityEventType type, Guid? targetUserId = null, Guid? roleId = null, Guid? sessionId = null, object? details = null)
    {
        var context = http.HttpContext;
        db.Set<SecurityEvent>().Add(SecurityEvent.Record(
            type, clock.GetUtcNow().UtcDateTime, currentUser.UserId, targetUserId, roleId, sessionId,
            context?.Connection.RemoteIpAddress?.ToString(), RequestCorrelation.Of(context),
            details is null ? null : JsonSerializer.Serialize(details, Json)));
    }
}
