namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>What every generated file states about itself: language, issuer, logo and when it was produced.</summary>
/// <param name="GeneratedAt">The company's local time (its time zone), offset included.</param>
public sealed record DocumentContext(DocumentLanguage Language, DocumentIssuer? Issuer, DateTimeOffset GeneratedAt, string LogoSvg)
{
    public bool IsArabic => Language == DocumentLanguage.Ar;

    /// <summary>The issuer's name in the document's language, falling back to the Arabic name.</summary>
    public string? CompanyName => Issuer is null ? null : IsArabic ? Issuer.NameAr : Issuer.NameEn ?? Issuer.NameAr;

    /// <summary>Picks the Arabic or the English text.</summary>
    public string L(string ar, string en) => IsArabic ? ar : en;

    /// <summary>A name in the document's language: the English one falls back to the Arabic one.</summary>
    public string Name(string nameAr, string? nameEn) => IsArabic ? nameAr : nameEn ?? nameAr;

    /// <summary>A stored UTC time in the company's time zone, as documents print it.</summary>
    public DateTime ToLocal(DateTime utc)
    {
        var value = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return Issuer is not null && TimeZoneInfo.TryFindSystemTimeZoneById(Issuer.TimeZoneId, out var zone)
            ? TimeZoneInfo.ConvertTimeFromUtc(value, zone)
            : value;
    }
}
