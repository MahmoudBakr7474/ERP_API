using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;

/// <param name="UserId">Events where this user acted or was the target.</param>
/// <param name="FromUtc">Inclusive lower bound.</param>
/// <param name="ToUtc">Exclusive upper bound.</param>
public sealed record SecurityEventFilter(Guid? UserId, SecurityEventType? Type, DateTime? FromUtc, DateTime? ToUtc);
