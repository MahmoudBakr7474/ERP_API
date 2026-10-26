using WarehouseERP.Domain.Common;
namespace WarehouseERP.Domain.Organization;
public sealed class Company : MasterEntity
{
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public string BaseCurrencyCode { get; set; } = null!;
    public string TimeZoneId { get; set; } = null!;
    public string? TaxRegistrationNumber { get; set; }
    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}
