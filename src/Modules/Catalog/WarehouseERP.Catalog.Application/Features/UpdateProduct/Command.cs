namespace WarehouseERP.Catalog.Application.Features.UpdateProduct;

/// <param name="RowVersion">Base64 row version from the last read; a stale one is a conflict.</param>
public sealed record UpdateProductCommand(Guid ProductId, string NameAr, string? NameEn, string RowVersion);
