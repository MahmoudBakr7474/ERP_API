using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Domain;

public sealed class Warehouse : MasterEntity
{
    public const string DefaultLocationCode = "DEFAULT";

    private readonly List<WarehouseLocation> _locations = [];

    private Warehouse() { } // EF Core

    public Guid BranchId { get; private set; }
    public Branch Branch { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    public WarehouseType WarehouseType { get; private set; }
    public bool AllowsSelling { get; private set; }
    public IReadOnlyCollection<WarehouseLocation> Locations => _locations;

    /// <summary>
    /// A new warehouse always comes with its DEFAULT location, saved with it in one SaveChanges.
    /// A Transit warehouse can never sell.
    /// </summary>
    public static Result<Warehouse> Create(
        Guid branchId, string code, string nameAr, string? nameEn, WarehouseType type, bool allowsSelling)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        if (type == WarehouseType.Transit && allowsSelling)
            return WarehouseDomainErrors.TransitCannotSell;

        var warehouse = new Warehouse
        {
            BranchId = branchId,
            Code = code.Trim(),
            NameAr = nameAr.Trim(),
            NameEn = Text.Optional(nameEn),
            WarehouseType = type,
            AllowsSelling = allowsSelling,
        };
        warehouse._locations.Add(WarehouseLocation.CreateDefault(warehouse));
        return warehouse;
    }
}
