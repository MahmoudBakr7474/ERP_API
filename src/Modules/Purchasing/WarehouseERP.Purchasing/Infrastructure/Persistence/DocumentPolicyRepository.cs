using Microsoft.EntityFrameworkCore;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.DocumentPolicies;
using WarehouseERP.Purchasing.Application.Contracts.PurchasingPolicies;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence;

internal sealed class DocumentPolicyRepository(DbContext db) : IDocumentPolicyRepository
{
    public Task<bool> RequiresApprovalAsync(PurchasingDocumentType documentType, CancellationToken ct) =>
        db.Set<DocumentPolicy>().Where(x => x.DocumentType == documentType).Select(x => x.RequiresApproval).SingleAsync(ct);

    public async Task<IReadOnlyList<PurchasingPolicyResponse>> ListAsync(CancellationToken ct) =>
        (await db.Set<DocumentPolicy>().AsNoTracking().OrderBy(x => x.DocumentType).ToListAsync(ct))
        .Select(x => new PurchasingPolicyResponse(x.DocumentType, x.RequiresApproval, Convert.ToBase64String(x.RowVersion)))
        .ToList();

    public async Task<DocumentPolicy?> GetForUpdateAsync(PurchasingDocumentType documentType, byte[] expectedRowVersion, CancellationToken ct)
    {
        var policy = await db.Set<DocumentPolicy>().SingleOrDefaultAsync(x => x.DocumentType == documentType, ct);
        if (policy is not null)
            db.Entry(policy).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return policy;
    }

    public Task<Result> SaveChangesAsync(CancellationToken ct) => PurchasingSaves.SaveAsync(db, ct);
}
