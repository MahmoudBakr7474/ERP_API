using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// A supplier: master data, versioned in a temporal table with its contacts and addresses. The code is fixed once
/// created. Registration numbers are kept normalized (letters and digits only) and are unique within their jurisdiction:
/// the tax registration number per country, the commercial registration number per country and registry office. An
/// inactive supplier takes no new orders; orders already under way may still be received, invoiced and returned.
/// </summary>
public sealed class Supplier : MasterEntity
{
    public const int MaxContacts = 20;
    public const int MaxAddresses = 10;

    private readonly List<SupplierContact> _contacts = [];
    private readonly List<SupplierAddress> _addresses = [];

    private Supplier() { } // EF Core

    public string Code { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }

    /// <summary>The Arabic name normalized for duplicate warnings (SupplierNames.Key).</summary>
    public string NameArKey { get; private set; } = null!;

    /// <summary>The English name normalized for duplicate warnings, when there is one.</summary>
    public string? NameEnKey { get; private set; }

    /// <summary>ISO 3166 alpha-2 country whose registrations the numbers below are.</summary>
    public string CountryCode { get; private set; } = null!;

    public string? TaxRegistrationNumber { get; private set; }
    public string? CommercialRegistrationNumber { get; private set; }

    /// <summary>The registry office that issued the commercial registration, as entered.</summary>
    public string? CommercialRegistry { get; private set; }

    /// <summary>The registry office normalized, which scopes the commercial registration number's uniqueness.</summary>
    public string? CommercialRegistryKey { get; private set; }

    /// <summary>Days from invoice date to due date that orders start with.</summary>
    public int PaymentTermDays { get; private set; }

    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Notes { get; private set; }
    public IReadOnlyList<SupplierContact> Contacts => _contacts;
    public IReadOnlyList<SupplierAddress> Addresses => _addresses;

    public static Result<Supplier> Create(string code, SupplierDetails details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var supplier = new Supplier { Code = code.Trim().ToUpperInvariant() };
        var applied = supplier.Apply(details);
        return applied.IsFailure ? applied.Error : supplier;
    }

    /// <summary>Everything but the code changes; contacts and addresses are replaced by the ones given.</summary>
    public Result Update(SupplierDetails details) => Apply(details);

    private Result Apply(SupplierDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentException.ThrowIfNullOrWhiteSpace(details.NameAr);
        var country = details.CountryCode.Trim().ToUpperInvariant();
        if (country.Length != 2 || !country.All(char.IsAsciiLetterUpper))
            return SupplierDomainErrors.CountryInvalid;

        var taxNumber = RegistrationNumbers.Normalize(details.TaxRegistrationNumber);
        if (taxNumber is not null && !RegistrationNumbers.IsValidTaxNumber(country, taxNumber))
            return country == RegistrationNumbers.Egypt ? SupplierDomainErrors.EgyptianTaxNumberInvalid : SupplierDomainErrors.TaxNumberInvalid;
        var registration = RegistrationNumbers.Normalize(details.CommercialRegistrationNumber);
        if (registration is not null && !RegistrationNumbers.IsValid(registration))
            return SupplierDomainErrors.CommercialRegistrationInvalid;
        var registry = Text.Optional(details.CommercialRegistry);
        if (registration is not null && registry is null)
            return SupplierDomainErrors.CommercialRegistryRequired;
        if (details.PaymentTermDays is < 0 or > 365)
            return SupplierDomainErrors.PaymentTermsInvalid;
        if (details.Contacts.Count > MaxContacts)
            return SupplierDomainErrors.TooManyContacts;
        if (details.Addresses.Count > MaxAddresses)
            return SupplierDomainErrors.TooManyAddresses;
        if (details.Contacts.Count(c => c.IsPrimary) > 1)
            return SupplierDomainErrors.SeveralPrimaryContacts;
        if (details.Addresses.Count(a => a.IsPrimary) > 1)
            return SupplierDomainErrors.SeveralPrimaryAddresses;
        var addresses = new List<SupplierAddress>(details.Addresses.Count);
        foreach (var address in details.Addresses)
        {
            var created = SupplierAddress.Create(address);
            if (created.IsFailure)
                return created.Error;
            addresses.Add(created.Value);
        }

        NameAr = details.NameAr.Trim();
        NameEn = Text.Optional(details.NameEn);
        NameArKey = SupplierNames.Key(NameAr);
        NameEnKey = NameEn is null ? null : SupplierNames.Key(NameEn);
        CountryCode = country;
        TaxRegistrationNumber = taxNumber;
        CommercialRegistrationNumber = registration;
        CommercialRegistry = registration is null ? null : registry;
        CommercialRegistryKey = registration is null ? null : SupplierNames.Key(registry!);
        PaymentTermDays = details.PaymentTermDays;
        Phone = Text.Optional(details.Phone);
        Email = Text.Optional(details.Email);
        Notes = Text.Optional(details.Notes);
        _contacts.Clear();
        _contacts.AddRange(details.Contacts.Select(SupplierContact.Create));
        _addresses.Clear();
        _addresses.AddRange(addresses);
        return Result.Success();
    }
}
