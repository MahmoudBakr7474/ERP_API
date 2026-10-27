using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Categories;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Categories.SetCategoryActive;

// A category is retired after its subcategories and reactivated after its parent, so an inactive category never holds
// an active one. Products may stay in a retired category; new assignments to it are refused.
public sealed partial class SetCategoryActiveHandler(ICategoryRepository categories, ILogger<SetCategoryActiveHandler> logger)
    : ICommandHandler<SetCategoryActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetCategoryActiveCommand command, CancellationToken ct)
    {
        var category = await categories.GetForUpdateAsync(command.CategoryId, expectedRowVersion: null, ct);
        if (category is null)
            return CategoryErrors.NotFound;
        if (!command.IsActive && await categories.HasActiveChildrenAsync(category.Id, ct))
            return CategoryErrors.HasActiveChildren;
        if (command.IsActive && category.ParentId is { } parentId && !await categories.IsActiveAsync(parentId, ct))
            return CategoryErrors.ParentNotActive;

        if (command.IsActive) category.Activate();
        else category.Deactivate();
        var saved = await categories.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ActiveChanged(logger, category.Id, command.IsActive);
        return Done.Value;
    }

    [LoggerMessage(EventId = 3008, Level = LogLevel.Information, Message = "Category {CategoryId} active set to {IsActive}")]
    private static partial void ActiveChanged(ILogger logger, Guid categoryId, bool isActive);
}
