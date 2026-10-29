using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

/// <summary>
/// A customer: master data shared by the whole company, versioned in a temporal table with its contacts and addresses.
/// The code and the type (individual or business) are fixed once created. Registration numbers are kept normalized and
/// are unique within their jurisdiction (tax number per country, commercial registration per country and registry office);
/// only a business carries a commercial registration, and an Egyptian tax number has 9 digits. Credit terms (payment
/// days, an optional credit limit and a manual credit hold with its reason) change only through <see cref="SetCredit"/>,
/// which records who changed them and when. An inactive customer takes no new quotations or orders; documents already
/// under way are delivered, invoiced and returned.
/// </summary>
public sealed class Customer : MasterEntity
{
    public const int MaxContacts = 20;
    public const int MaxAddresses = 10;

    private readonly List<CustomerContact> _contacts = [];
    private readonly List<CustomerAddress> _addresses = [];

    private Customer() { } // EF Core

    public string Code { get; private set; } = null!;
    public CustomerType CustomerType { get; private set; }
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }

    /// <summary>The Arabic name normalized for duplicate warnings (CustomerNames.Key).</summary>
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

    public string? Phone { get; private set; }
    public string? Mobile { get; private set; }

    /// <summary>The mobile number's digits, for duplicate warnings.</summary>
    public string? MobileKey { get; private set; }

    public string? Email { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>Days from invoice date to due date that new orders start with; 0 is cash.</summary>
    public int PaymentTermDays { get; private set; }

    /// <summary>The credit the company is willing to extend, in the base currency; null: no limit set.</summary>
    public decimal? CreditLimit { get; private set; }

    /// <summary>Set by hand; what it blocks depends on the sales settings.</summary>
    public bool IsOnCreditHold { get; private set; }

    public string? CreditHoldReason { get; private set; }
    public DateTime? CreditChangedAtUtc { get; private set; }
    public Guid? CreditChangedByUserId { get; private set; }
    public IReadOnlyList<CustomerContact> Contacts => _contacts;
    public IReadOnlyList<CustomerAddress> Addresses => _addresses;

    public static Result<Customer> Create(string code, CustomerType customerType, CustomerDetails details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (!Enum.IsDefined(customerType))
            return CustomerDomainErrors.TypeInvalid;
        var customer = new Customer { Code = code.Trim().ToUpperInvariant(), CustomerType = customerType };
        var applied = customer.Apply(details);
        return applied.IsFailure ? applied.Error : customer;
    }

    /// <summary>Everything but the code, the type and the credit terms changes; contacts and addresses are replaced.</summary>
    public Result Update(CustomerDetails details) => Apply(details);

    /// <summary>Changes the credit terms; a hold needs its reason.</summary>
    public Result SetCredit(int paymentTermDays, decimal? creditLimit, bool onHold, string? holdReason, Guid? userId, DateTime nowUtc)
    {
        if (paymentTermDays is < 0 or > 365)
            return CustomerDomainErrors.PaymentTermsInvalid;
        if (creditLimit is { } limit && (limit < 0 || limit > Amounts.MaxLineAmount || !Amounts.FitsScale(limit, 4)))
            return CustomerDomainErrors.CreditLimitInvalid;
        var reason = Text.Optional(holdReason);
        if (onHold && reason is null)
            return CustomerDomainErrors.CreditHoldReasonRequired;

        PaymentTermDays = paymentTermDays;
        CreditLimit = creditLimit;
        IsOnCreditHold = onHold;
        CreditHoldReason = onHold ? reason : null;
        CreditChangedAtUtc = nowUtc;
        CreditChangedByUserId = userId;
        return Result.Success();
    }

    private Result Apply(CustomerDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentException.ThrowIfNullOrWhiteSpace(details.NameAr);
        var country = details.CountryCode.Trim().ToUpperInvariant();
        if (country.Length != 2 || !country.All(char.IsAsciiLetterUpper))
            return CustomerDomainErrors.CountryInvalid;

        var taxNumber = RegistrationNumbers.Normalize(details.TaxRegistrationNumber);
        if (taxNumber is not null && !RegistrationNumbers.IsValidTaxNumber(country, taxNumber))
            return country == RegistrationNumbers.Egypt ? CustomerDomainErrors.EgyptianTaxNumberInvalid : CustomerDomainErrors.TaxNumberInvalid;
        var registration = RegistrationNumbers.Normalize(details.CommercialRegistrationNumber);
        if (registration is not null && CustomerType != CustomerType.Business)
            return CustomerDomainErrors.CommercialRegistrationForBusinessOnly;
        if (registration is not null && !RegistrationNumbers.IsValid(registration))
            return CustomerDomainErrors.CommercialRegistrationInvalid;
        var registry = Text.Optional(details.CommercialRegistry);
        if (registration is not null && registry is null)
            return CustomerDomainErrors.CommercialRegistryRequired;
        if (details.Contacts.Count > MaxContacts)
            return CustomerDomainErrors.TooManyContacts;
        if (details.Addresses.Count > MaxAddresses)
            return CustomerDomainErrors.TooManyAddresses;
        if (details.Contacts.Count(c => c.IsPrimary) > 1)
            return CustomerDomainErrors.SeveralPrimaryContacts;
        if (details.Addresses.Count(a => a.IsPrimary) > 1)
            return CustomerDomainErrors.SeveralPrimaryAddresses;
        NameAr = details.NameAr.Trim();
        NameEn = Text.Optional(details.NameEn);
        NameArKey = CustomerNames.Key(NameAr);
        NameEnKey = NameEn is null ? null : CustomerNames.Key(NameEn);
        CountryCode = country;
        TaxRegistrationNumber = taxNumber;
        CommercialRegistrationNumber = registration;
        CommercialRegistry = registration is null ? null : registry;
        CommercialRegistryKey = registration is null ? null : CustomerNames.Key(registry!);
        Phone = Text.Optional(details.Phone);
        Mobile = Text.Optional(details.Mobile);
        MobileKey = CustomerNames.PhoneKey(Mobile);
        Email = Text.Optional(details.Email);
        Notes = Text.Optional(details.Notes);
        _contacts.Clear();
        _contacts.AddRange(details.Contacts.Select(CustomerContact.Create));
        _addresses.Clear();
        _addresses.AddRange(details.Addresses.Select(CustomerAddress.Create));
        return Result.Success();
    }
}
