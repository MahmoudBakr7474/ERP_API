using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Organization.Domain;

public sealed class Branch : MasterEntity
{
    private readonly List<Warehouse> _warehouses = [];

    private Branch() { } // EF Core

    public Guid CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    public IReadOnlyCollection<Warehouse> Warehouses => _warehouses;

    internal static Branch Create(Company company, string code, string nameAr, string? nameEn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        return new Branch { Company = company, Code = code.Trim(), NameAr = nameAr.Trim(), NameEn = Text.Optional(nameEn) };
    }
}
