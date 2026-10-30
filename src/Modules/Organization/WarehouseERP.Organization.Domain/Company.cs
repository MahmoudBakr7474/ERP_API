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
    public string? CommercialRegistrationNumber { get; private set; }
    public string? CommercialRegistry { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? AddressLine { get; private set; }
    public Guid? CityId { get; private set; }
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

    /// <summary>
    /// Code and base currency are fixed once the company is set up: documents, balances and costs are kept in
    /// that currency.
    /// </summary>
    public void UpdateSettings(string nameAr, string? nameEn, string timeZoneId, string? taxRegistrationNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);
        NameAr = nameAr.Trim();
        NameEn = Text.Optional(nameEn);
        TimeZoneId = timeZoneId.Trim();
        TaxRegistrationNumber = Text.Optional(taxRegistrationNumber);
    }

    /// <summary>
    /// The identification and contact details official documents print (commercial registration, address, phone,
    /// e-mail). All optional: what is not known stays empty and is never printed as if it were.
    /// </summary>
    public void SetContactDetails(CompanyContactDetails details)
    {
        CommercialRegistrationNumber = Text.Optional(details.CommercialRegistrationNumber);
        CommercialRegistry = Text.Optional(details.CommercialRegistry);
        Phone = Text.Optional(details.Phone);
        Email = Text.Optional(details.Email);
        AddressLine = Text.Optional(details.AddressLine);
        CityId = details.CityId;
    }

    public Branch AddBranch(string code, string nameAr, string? nameEn)
    {
        var branch = Branch.Create(this, code, nameAr, nameEn);
        _branches.Add(branch);
        return branch;
    }
}
