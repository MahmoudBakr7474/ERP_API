using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Infrastructure.Persistence;

// The save point every Sales repository shares (one DbContext, one SaveChanges per use case; Inventory's stock document
// and reservations, posted inside the same transaction, save with it). A known unique-index violation comes back as its
// business conflict; anything else is rethrown. Changing only a document's lines or history (or a customer's contacts
// and addresses) would leave its own row, and so its row version, untouched: marking a real column modified makes the
// save write the row, check the version the client sent and renew it.
internal static class SalesSaves
{
    // Unique indexes whose violation is an expected business conflict. Names come from Migration10.
    private static readonly Dictionary<string, Error> KnownUniqueConstraints = new(StringComparer.Ordinal)
    {
        ["UX_Customers_Code"] = CustomerErrors.CodeTaken,
        ["UX_Customers_TaxNumber"] = CustomerErrors.TaxNumberTaken,
        ["UX_Customers_CommercialRegistration"] = CustomerErrors.CommercialRegistrationTaken,
        ["UX_SalesPrices_ProductUnit"] = PriceErrors.AlreadyPriced,
    };

    public static Task<Result> SaveAsync(DbContext db, CancellationToken ct)
    {
        TouchChangedRoots(db);
        return DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
    }

    private static void TouchChangedRoots(DbContext db)
    {
        db.ChangeTracker.DetectChanges();
        var changed = new HashSet<Guid>();
        foreach (var entry in db.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;
            Guid? root = entry.Entity switch
            {
                SalesQuotationLine line => line.QuotationId,
                SalesOrderLine line => line.OrderId,
                SalesDeliveryLine line => line.DeliveryId,
                SalesInvoiceLine line => line.InvoiceId,
                SalesReturnLine line => line.ReturnId,
                DocumentEvent documentEvent => documentEvent.DocumentId,
                CustomerContact contact => contact.CustomerId,
                CustomerAddress address => address.CustomerId,
                _ => null,
            };
            if (root is { } id)
                changed.Add(id);
        }

        foreach (var entry in db.ChangeTracker.Entries<IAuditable>())
            if (entry.State is EntityState.Unchanged or EntityState.Modified && IdOf(entry.Entity) is { } id && changed.Contains(id))
                entry.Property(nameof(IAuditable.UpdatedAtUtc)).IsModified = true;
    }

    private static Guid? IdOf(IAuditable entity) => entity switch
    {
        SalesQuotation quotation => quotation.Id,
        SalesOrder order => order.Id,
        SalesDelivery delivery => delivery.Id,
        SalesInvoice invoice => invoice.Id,
        SalesReturn salesReturn => salesReturn.Id,
        Customer customer => customer.Id,
        _ => null,
    };
}
