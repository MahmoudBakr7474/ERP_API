namespace WarehouseERP.Domain.Common;

// Master-data edits are versioned in SQL Server temporal history tables.
// AuditLog is reserved for operational business actions (future migrations).
public abstract class MasterEntity
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
