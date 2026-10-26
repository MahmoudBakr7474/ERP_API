using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Organization.Domain;

public sealed class Company : MasterEntity
{
    private readonly List<Branch> _branches = [];

    private Company() { } // EF Core

    public string Code { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    public string BaseCurrencyCode { get; private set; } = null!;
    public string TimeZoneId { get; private set; } = null!;
    public string? TaxRegistrationNumber { get; private set; }
    public IReadOnlyCollection<Branch> Branches => _branches;

    public static Company Create(
        string code, string nameAr, string? nameEn, string baseCurrencyCode, string timeZoneId, string? taxRegistrationNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseCurrencyCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);
        return new Company
        {
            Code = code.Trim(),
            NameAr = nameAr.Trim(),
            NameEn = Text.Optional(nameEn),
            BaseCurrencyCode = baseCurrencyCode.Trim().ToUpperInvariant(),
            TimeZoneId = timeZoneId.Trim(),
            TaxRegistrationNumber = Text.Optional(taxRegistrationNumber),
        };
    }

    public Branch AddBranch(string code, string nameAr, string? nameEn)
    {
        var branch = Branch.Create(this, code, nameAr, nameEn);
        _branches.Add(branch);
        return branch;
    }
}
