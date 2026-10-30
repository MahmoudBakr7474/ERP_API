using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Accounting.Application.Shared;

// Business dates are the company's local dates (its time zone): an operational document's accounting date is the local
// date it posted on, and the cutover starts at local midnight of the accounting start date.
internal static class BusinessDates
{
    public static TimeZoneInfo ZoneOf(CompanyInfo company) => TimeZoneInfo.FindSystemTimeZoneById(company.TimeZoneId);

    public static DateOnly LocalDate(DateTime utc, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone));

    public static DateTime StartUtc(DateOnly date, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), zone);
}
