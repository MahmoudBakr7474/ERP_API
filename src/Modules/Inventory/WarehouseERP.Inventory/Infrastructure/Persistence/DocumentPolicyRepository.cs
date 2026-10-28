using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.DocumentPolicies;
using WarehouseERP.Inventory.Application.Contracts.DocumentPolicies;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Infrastructure.Persistence;

internal sealed class DocumentPolicyRepository(DbContext db) : IDocumentPolicyRepository
{
    // A type without a policy row needs approval: the safe side.
    public async Task<bool> RequiresApprovalAsync(StockDocumentType documentType, CancellationToken ct) =>
        documentType == StockDocumentType.Reversal
        || (await db.Set<DocumentTypePolicy>().AsNoTracking()
            .Where(x => x.DocumentType == documentType)
            .Select(x => (bool?)x.RequiresApproval)
            .SingleOrDefaultAsync(ct) ?? true);

    public async Task<IReadOnlyList<DocumentPolicyResponse>> ListAsync(CancellationToken ct) =>
        (await db.Set<DocumentTypePolicy>().AsNoTracking().OrderBy(x => x.DocumentType).ToListAsync(ct))
        .Select(x => new DocumentPolicyResponse(x.DocumentType, x.RequiresApproval, x.UpdatedAtUtc, Convert.ToBase64String(x.RowVersion)))
        .ToList();

    public async Task<DocumentTypePolicy?> GetForUpdateAsync(StockDocumentType documentType, byte[] expectedRowVersion, CancellationToken ct)
    {
        var policy = await db.Set<DocumentTypePolicy>().SingleOrDefaultAsync(x => x.DocumentType == documentType, ct);
        if (policy is not null)
            db.Entry(policy).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return policy;
    }

    public Task<Result> SaveChangesAsync(CancellationToken ct) => DbFailures.SaveAsync(db, NoKnownConstraints, ct);

    private static readonly Dictionary<string, Error> NoKnownConstraints = new(StringComparer.Ordinal);
}
