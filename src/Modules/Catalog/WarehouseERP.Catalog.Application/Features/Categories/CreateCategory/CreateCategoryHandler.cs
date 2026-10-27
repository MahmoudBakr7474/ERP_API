using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Categories;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Categories.CreateCategory;

// A new category goes under an active parent, or at the top. One save point; the database still guards the code.
public sealed partial class CreateCategoryHandler(ICategoryRepository categories, ILogger<CreateCategoryHandler> logger)
    : ICommandHandler<CreateCategoryCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateCategoryCommand command, CancellationToken ct)
    {
        if (command.ParentId is { } parentId && !await categories.IsActiveAsync(parentId, ct))
            return CategoryErrors.ParentNotActive;
        if (await categories.CodeExistsAsync(command.Code.Trim(), ct))
            return CategoryErrors.CodeTaken;

        var category = Category.Create(command.Code, command.NameAr, command.NameEn, command.ParentId);
        var saved = await categories.AddAsync(category, ct);
        if (saved.IsFailure)
            return saved.Error;

        CategoryCreated(logger, category.Id, category.Code);
        return category.Id;
    }

    [LoggerMessage(EventId = 3006, Level = LogLevel.Information, Message = "Category {CategoryId} ({CategoryCode}) created")]
    private static partial void CategoryCreated(ILogger logger, Guid categoryId, string categoryCode);
}
