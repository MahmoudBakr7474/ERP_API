using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Catalog.Domain;

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
