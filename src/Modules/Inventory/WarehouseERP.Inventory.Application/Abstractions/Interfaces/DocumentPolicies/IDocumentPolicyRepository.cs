using WarehouseERP.Inventory.Application.Contracts.DocumentPolicies;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.DocumentPolicies;

public interface IDocumentPolicyRepository
{
    /// <summary>Whether a new document of this type needs approval; a reversal always does.</summary>
    Task<bool> RequiresApprovalAsync(StockDocumentType documentType, CancellationToken ct);

    Task<IReadOnlyList<DocumentPolicyResponse>> ListAsync(CancellationToken ct);

    /// <summary>The policy, tracked for changes, or null; the save fails with a conflict unless it still has this row version.</summary>
    Task<DocumentTypePolicy?> GetForUpdateAsync(StockDocumentType documentType, byte[] expectedRowVersion, CancellationToken ct);

    Task<Result> SaveChangesAsync(CancellationToken ct);
}
