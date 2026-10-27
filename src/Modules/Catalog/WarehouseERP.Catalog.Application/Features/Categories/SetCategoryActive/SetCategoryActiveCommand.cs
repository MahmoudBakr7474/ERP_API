namespace WarehouseERP.Catalog.Application.Features.Categories.SetCategoryActive;

public sealed record SetCategoryActiveCommand(Guid CategoryId, bool IsActive);
