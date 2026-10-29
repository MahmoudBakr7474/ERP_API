using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Identity.Contracts;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Application.Contracts.Customers;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.ReadModels;

// Address names (city, governorate, country) come from Organization's lookups as they are now.
internal sealed class CustomerReadStore(DbContext db, IGeographyDirectory geography, IUserDirectory users) : ICustomerReadStore
{
    private const int MaxSimilar = 20;

    public async Task<CustomerDetailsResponse?> GetAsync(Guid customerId, CancellationToken ct)
    {
        var customer = await db.Set<Customer>().AsNoTracking()
            .Include(x => x.Contacts)
            .Include(x => x.Addresses)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == customerId, ct);
        if (customer is null)
            return null;

        var cities = (await geography.FindCitiesAsync(customer.Addresses.Select(a => a.CityId).ToList(), ct)).ToDictionary(c => c.Id);
        var changedBy = customer.CreditChangedByUserId is { } userId
            ? (await users.GetDisplayNamesAsync([userId], ct)).GetValueOrDefault(userId)
            : null;
        return new CustomerDetailsResponse(
            customer.Id, customer.Code, customer.CustomerType, customer.NameAr, customer.NameEn, customer.CountryCode,
            customer.TaxRegistrationNumber, customer.CommercialRegistrationNumber, customer.CommercialRegistry, customer.Phone,
            customer.Mobile, customer.Email, customer.Notes, customer.PaymentTermDays, customer.CreditLimit, customer.IsOnCreditHold,
            customer.CreditHoldReason, customer.CreditChangedAtUtc, changedBy, customer.IsActive,
            customer.Contacts.OrderByDescending(c => c.IsPrimary).ThenBy(c => c.Name)
                .Select(c => new CustomerContactResponse(c.Id, c.Name, c.JobTitle, c.Phone, c.Email, c.IsPrimary)).ToList(),
            customer.Addresses.OrderByDescending(a => a.IsPrimary).ThenBy(a => a.AddressType)
                .Select(a => Address(a, cities[a.CityId]))
                .ToList(),
            customer.CreatedAtUtc, customer.UpdatedAtUtc, Convert.ToBase64String(customer.RowVersion));
    }

    // An address's city always exists: the foreign key restricts deleting it, and the lookups are never hard-deleted.
    private static CustomerAddressResponse Address(CustomerAddress a, CityInfo city) => new(
        a.Id, a.AddressType, a.Line1, a.Line2, city.CountryCode, city.CountryNameAr, city.CountryNameEn,
        city.GovernorateId, city.GovernorateNameAr, city.GovernorateNameEn, city.Id, city.NameAr, city.NameEn, a.PostalCode, a.IsPrimary);

    public async Task<PagedResult<CustomerSummaryResponse>> ListAsync(CustomerFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<Customer>().AsNoTracking();
        if (filter.IsActive is { } isActive)
            query = query.Where(x => x.IsActive == isActive);
        if (filter.CustomerType is { } customerType)
            query = query.Where(x => x.CustomerType == customerType);
        if (filter.IsOnCreditHold is { } onHold)
            query = query.Where(x => x.IsOnCreditHold == onHold);
        if (filter.CountryCode is { } country)
            query = query.Where(x => x.CountryCode == country);
        if (filter.Search is { } search)
        {
            var phone = CustomerNames.PhoneKey(search);
            query = query.Where(x => x.Code.Contains(search) || x.NameAr.Contains(search) || (x.NameEn != null && x.NameEn.Contains(search))
                                     || (x.TaxRegistrationNumber != null && x.TaxRegistrationNumber.Contains(search))
                                     || (x.Phone != null && x.Phone.Contains(search))
                                     || (phone != null && x.MobileKey != null && x.MobileKey.Contains(phone)));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Code)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new CustomerSummaryResponse(
                x.Id, x.Code, x.CustomerType, x.NameAr, x.NameEn, x.CountryCode, x.TaxRegistrationNumber, x.Mobile, x.Phone,
                x.PaymentTermDays, x.CreditLimit, x.IsOnCreditHold, x.IsActive))
            .ToListAsync(ct);
        return new PagedResult<CustomerSummaryResponse>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<SimilarCustomerResponse>> FindSimilarAsync(
        IReadOnlyCollection<string> nameKeys, string? mobileKey, Guid? exceptCustomerId, CancellationToken ct)
    {
        var keys = nameKeys.ToArray();
        return await db.Set<Customer>().AsNoTracking()
            .Where(x => (keys.Contains(x.NameArKey) || (x.NameEnKey != null && keys.Contains(x.NameEnKey))
                         || (mobileKey != null && x.MobileKey == mobileKey))
                        && (exceptCustomerId == null || x.Id != exceptCustomerId))
            .OrderBy(x => x.Code)
            .Take(MaxSimilar)
            .Select(x => new SimilarCustomerResponse(x.Id, x.Code, x.NameAr, x.NameEn, x.Mobile, x.TaxRegistrationNumber, x.IsActive))
            .ToListAsync(ct);
    }

    // Each committed order line still to be billed counts at its agreed total per unit (net plus tax): what is neither
    // invoiced, closed nor returned before invoicing. Posted invoices and credits are summed as they are.
    public async Task<CustomerOrderPosition?> GetOrderPositionAsync(Guid customerId, CancellationToken ct)
    {
        if (!await db.Set<Customer>().AnyAsync(x => x.Id == customerId, ct))
            return null;
        SalesOrderStatus[] committed =
            [SalesOrderStatus.Confirmed, SalesOrderStatus.PartiallyDelivered, SalesOrderStatus.Delivered, SalesOrderStatus.Closed];
        var orders = await db.Set<SalesOrder>().AsNoTracking()
            .Where(o => o.CustomerId == customerId && committed.Contains(o.Status))
            .Include(o => o.Lines)
            .AsSplitQuery()
            .ToListAsync(ct);

        decimal uninvoiced = 0m, deliveredNotInvoiced = 0m;
        foreach (var line in orders.SelectMany(o => o.Lines))
        {
            var perUnit = line.TotalAmount / line.Quantity;
            var unbilled = line.Quantity - line.ClosedQuantity - line.InvoicedQuantity - (line.ReturnedQuantity - line.ReturnedInvoicedQuantity);
            uninvoiced += Math.Max(unbilled, 0m) * perUnit;
            deliveredNotInvoiced += Math.Max(line.InvoiceableQuantity, 0m) * perUnit;
        }
        var openCount = orders.Count(o => o.IsOpen);
        var invoiced = await db.Set<SalesInvoice>().AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.Status == SalesInvoiceStatus.Posted)
            .SumAsync(i => (decimal?)i.TotalAmount, ct) ?? 0m;
        var credits = await db.Set<SalesReturn>().AsNoTracking()
            .Where(r => r.CustomerId == customerId && r.Status == SalesReturnStatus.Approved)
            .SumAsync(r => (decimal?)r.CreditTotalAmount, ct) ?? 0m;
        return new CustomerOrderPosition(openCount, decimal.Round(uninvoiced, 2), decimal.Round(deliveredNotInvoiced, 2), invoiced, credits);
    }
}
