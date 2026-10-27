namespace WarehouseERP.Catalog.Application.Features.Products.SetProductActive;

public sealed record SetProductActiveCommand(Guid ProductId, bool IsActive);
