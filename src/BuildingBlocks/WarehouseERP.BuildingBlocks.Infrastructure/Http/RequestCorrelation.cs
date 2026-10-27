using Microsoft.AspNetCore.Http;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Http;

/// <summary>The request's correlation id (set by the host's correlation middleware), for modules that record it.</summary>
public static class RequestCorrelation
{
    public const string ItemKey = "CorrelationId";

    public static string? Of(HttpContext? http) => http?.Items[ItemKey] as string;
}
