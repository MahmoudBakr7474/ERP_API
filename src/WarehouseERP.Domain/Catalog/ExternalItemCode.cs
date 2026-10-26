using WarehouseERP.Domain.Common;
namespace WarehouseERP.Domain.Catalog;
public enum ExternalCodeType : byte { EGS = 1, GS1 = 2 }
public enum CodeApprovalStatus : byte { Pending = 1, Approved = 2, Rejected = 3 }
public sealed class ExternalItemCode : MasterEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid? ProductUnitId { get; set; }
    public ProductUnit? ProductUnit { get; set; }
    public ExternalCodeType CodeType { get; set; }
    public string Code { get; set; } = null!;
    public CodeApprovalStatus ApprovalStatus { get; set; } = CodeApprovalStatus.Pending;
}
