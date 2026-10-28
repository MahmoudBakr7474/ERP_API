using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// Whether purchase requests or purchase orders need approval. A document copies it when created, so changing the
/// policy never alters documents already under way. Supplier returns always need approval and invoices need it for
/// price differences; neither has a policy.
/// </summary>
public sealed class DocumentPolicy : IAuditable
{
    private DocumentPolicy() { } // EF Core

    public PurchasingDocumentType DocumentType { get; private set; }
    public bool RequiresApproval { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public void Change(bool requiresApproval) => RequiresApproval = requiresApproval;
}
