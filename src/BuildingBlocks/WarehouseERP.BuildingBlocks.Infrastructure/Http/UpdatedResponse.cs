namespace WarehouseERP.BuildingBlocks.Infrastructure.Http;

/// <summary>Body of a successful update: the new row version (base64) to send with the next update.</summary>
public sealed record UpdatedResponse(string RowVersion);
