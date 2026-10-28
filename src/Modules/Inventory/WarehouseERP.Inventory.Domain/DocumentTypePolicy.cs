using WarehouseERP.Inventory.Contracts;
using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// Whether documents of a type need approval before posting. A document copies it when created, so changing the policy
/// never alters documents already under way. A reversal always needs approval and has no policy.
/// </summary>
public sealed class DocumentTypePolicy : IAuditable
{
    private DocumentTypePolicy() { } // EF Core

    public StockDocumentType DocumentType { get; private set; }
    public bool RequiresApproval { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public void Change(bool requiresApproval) => RequiresApproval = requiresApproval;
}
