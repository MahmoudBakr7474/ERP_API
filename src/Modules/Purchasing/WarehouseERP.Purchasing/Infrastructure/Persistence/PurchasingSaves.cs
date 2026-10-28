using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence;

// The save point every Purchasing repository shares (one DbContext, one SaveChanges per use case). A known unique-index
// violation comes back as its business conflict; anything else is rethrown. Changing only a document's lines or history
// (or a supplier's contacts and addresses) would leave its own row, and so its row version, untouched: marking a real
// column modified makes the save write the row, check the version the client sent and renew it.
internal static class PurchasingSaves
{
    // Unique indexes whose violation is an expected business conflict. Names come from Migration07.
    private static readonly Dictionary<string, Error> KnownUniqueConstraints = new(StringComparer.Ordinal)
    {
        ["UX_Suppliers_Code"] = SupplierErrors.CodeTaken,
        ["UX_Suppliers_TaxNumber"] = SupplierErrors.TaxNumberTaken,
        ["UX_Suppliers_CommercialRegistration"] = SupplierErrors.CommercialRegistrationTaken,
        ["UX_SupplierInvoices_SupplierNumber"] = SupplierInvoiceErrors.InvoiceNumberTaken,
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
                PurchaseOrderLine line => line.OrderId,
                PurchaseRequestLine line => line.RequestId,
                GoodsReceiptLine line => line.ReceiptId,
                SupplierInvoiceLine line => line.InvoiceId,
                SupplierReturnLine line => line.ReturnId,
                DocumentEvent documentEvent => documentEvent.DocumentId,
                SupplierContact contact => contact.SupplierId,
                SupplierAddress address => address.SupplierId,
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
        PurchaseOrder order => order.Id,
        PurchaseRequest request => request.Id,
        GoodsReceipt receipt => receipt.Id,
        SupplierInvoice invoice => invoice.Id,
        SupplierReturn supplierReturn => supplierReturn.Id,
        Supplier supplier => supplier.Id,
        _ => null,
    };
}
