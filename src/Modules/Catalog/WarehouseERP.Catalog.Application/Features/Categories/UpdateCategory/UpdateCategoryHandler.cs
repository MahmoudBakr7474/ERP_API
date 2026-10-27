using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Categories;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.Catalog.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Categories.UpdateCategory;

// The code is fixed; names change and the category may move. A move goes under an active parent that is neither the
// category nor below it, so the tree never loops. Returns the new row version for the caller's next update.
public sealed partial class UpdateCategoryHandler(ICategoryRepository categories, ILogger<UpdateCategoryHandler> logger)
    : ICommandHandler<UpdateCategoryCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateCategoryCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var category = await categories.GetForUpdateAsync(command.CategoryId, expected, ct);
        if (category is null)
            return CategoryErrors.NotFound;

        if (command.ParentId != category.ParentId)
        {
            if (command.ParentId is { } parentId)
            {
                if (await categories.IsInSubtreeAsync(category.Id, parentId, ct))
                    return CategoryErrors.Cycle;
                if (!await categories.IsActiveAsync(parentId, ct))
                    return CategoryErrors.ParentNotActive;
            }
            category.MoveTo(command.ParentId);
        }
        category.Rename(command.NameAr, command.NameEn);
        var saved = await categories.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        CategoryUpdated(logger, category.Id, category.ParentId);
        return Convert.ToBase64String(category.RowVersion);
    }

    [LoggerMessage(EventId = 3007, Level = LogLevel.Information, Message = "Category {CategoryId} updated (parent {ParentId})")]
    private static partial void CategoryUpdated(ILogger logger, Guid categoryId, Guid? parentId);
}
