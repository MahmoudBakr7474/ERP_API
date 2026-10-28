using WarehouseERP.Purchasing.Application.Contracts.PurchasingPolicies;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.DocumentPolicies;

public interface IDocumentPolicyRepository
{
    /// <summary>The current policy of purchase requests or purchase orders.</summary>
    Task<bool> RequiresApprovalAsync(PurchasingDocumentType documentType, CancellationToken ct);

    Task<IReadOnlyList<PurchasingPolicyResponse>> ListAsync(CancellationToken ct);

    /// <summary>The policy, tracked, or null; the save fails with a conflict unless it still has the row version.</summary>
    Task<DocumentPolicy?> GetForUpdateAsync(PurchasingDocumentType documentType, byte[] expectedRowVersion, CancellationToken ct);

    Task<Result> SaveChangesAsync(CancellationToken ct);
}
