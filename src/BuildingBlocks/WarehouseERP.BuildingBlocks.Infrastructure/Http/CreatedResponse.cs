namespace WarehouseERP.BuildingBlocks.Infrastructure.Http;

/// <summary>Body of a 201 Created: the new resource's id (its URL is in the Location header).</summary>
public sealed record CreatedResponse(Guid Id);
