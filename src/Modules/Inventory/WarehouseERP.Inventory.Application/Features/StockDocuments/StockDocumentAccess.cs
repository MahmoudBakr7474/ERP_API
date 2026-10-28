using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments;

// Who may touch a document: the permission follows its type (a reversal's, the type it reverses), the scope its
// warehouse. Scope comes before existence, so a scoped user can't tell a missing document from a hidden one.
internal static class StockDocumentAccess
{
    public static string ManagePermission(StockDocumentType documentType) => documentType switch
    {
        StockDocumentType.OpeningBalance => InventoryPermissions.OpeningManage,
        StockDocumentType.Receipt => InventoryPermissions.ReceiptsManage,
        StockDocumentType.Issue => InventoryPermissions.IssuesManage,
        StockDocumentType.Adjustment => InventoryPermissions.AdjustmentsManage,
        _ => throw new ArgumentOutOfRangeException(nameof(documentType), documentType, "This type has no manage permission."),
    };

    /// <summary>The document's header once the user may see it and, with <paramref name="manage"/>, manage its type.</summary>
    public static async Task<Result<StockDocumentHeader>> AuthorizeAsync(
        IStockDocumentRepository documents, ICurrentUser user, Guid documentId, bool manage, CancellationToken ct)
    {
        var header = await documents.FindHeaderAsync(documentId, ct);
        if (header is null)
            return user.HasAllWarehouses ? StockDocumentErrors.NotFound : StockDocumentErrors.AccessDenied;
        if (!user.CanAccessWarehouse(header.WarehouseId))
            return StockDocumentErrors.AccessDenied;
        if (manage && !user.HasPermission(ManagePermission(header.ReversedType ?? header.DocumentType)))
            return StockDocumentErrors.PermissionDenied;
        return header;
    }
}
