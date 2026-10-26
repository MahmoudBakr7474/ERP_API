namespace WarehouseERP.SharedKernel.Domain;

// Master-data edits are versioned in SQL Server temporal history tables.
// AuditLog is reserved for operational business actions (future migrations).
// Setters are private: EF Core sets Id and RowVersion, the auditing interceptor sets the audit
// fields, and domain methods change IsActive.
public abstract class MasterEntity : IAuditable
{
    public Guid Id { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
