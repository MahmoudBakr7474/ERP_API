using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments;

// Who may touch a document: the permission follows its type (a reversal's, the type it reverses), the scope its
// warehouse. Scope comes before existence, so a scoped user can't tell a missing document from a hidden one. Documents a
// transfer, a count or Purchasing produced have no manage permission: they change only where they came from.
internal static class StockDocumentAccess
{
    /// <summary>The permission that manages documents of this type, or null for a type no one drafts here.</summary>
    public static string? ManagePermission(StockDocumentType documentType) => documentType switch
    {
        StockDocumentType.OpeningBalance => InventoryPermissions.OpeningManage,
        StockDocumentType.Receipt => InventoryPermissions.ReceiptsManage,
        StockDocumentType.Issue => InventoryPermissions.IssuesManage,
        StockDocumentType.Adjustment => InventoryPermissions.AdjustmentsManage,
        _ => null,
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
        if (!manage)
            return header;
        var managedType = header.ReversedType ?? header.DocumentType;
        if (ManagePermission(managedType) is not { } permission)
            return StockDocumentErrors.NotManagedHere(managedType);
        if (!user.HasPermission(permission))
            return StockDocumentErrors.PermissionDenied;
        return header;
    }
}
