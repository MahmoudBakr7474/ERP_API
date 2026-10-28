using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.UnitTests.Purchasing;

// Purchasing's rules without a database: line amounts and their rounding, an order's approval (its creator may approve),
// receipts that never exceed the order, invoices that never exceed what was received, returns that split into goods not
// yet invoiced and a credit claim for goods already invoiced, requests that orders never overdraw, and the keys that
// catch duplicate suppliers and invoice numbers.
public sealed class PurchasingRulesTests
{
    private static readonly Guid Warehouse = Guid.NewGuid();
    private static readonly Guid Branch = Guid.NewGuid();
    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid Location = Guid.NewGuid();
    private static readonly Guid Creator = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DocumentNumber Number = new("PO-B-2026-000001", 2026, 1);

    private static ProductUnitFacts Pieces() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1m, false, false);

    private static LinePrice Price(decimal unitPrice, decimal discount = 0, decimal tax = 0, TaxTreatment treatment = TaxTreatment.Recoverable) =>
        new(unitPrice, discount, tax, treatment);

    // EF Core gives ids and the creator on save; these tests set them the same way.
    private static T Saved<T>(T entity) where T : class
    {
        typeof(T).GetProperty("Id")!.SetValue(entity, Guid.NewGuid());
        typeof(T).GetProperty("CreatedByUserId")?.SetValue(entity, Creator);
        return entity;
    }

    private static PurchaseOrder ApprovedOrder(decimal quantity, decimal unitPrice, decimal discount = 0, decimal tax = 0)
    {
        var order = Saved(PurchaseOrder.CreateDraft(Warehouse, Branch, "EGP", requiresApproval: true, new OrderTerms(SupplierId, null, null, 30, null),
            [new OrderLineInput(Pieces(), quantity, Price(unitPrice, discount, tax), null)], Creator, Now).Value);
        foreach (var line in order.Lines)
            Saved(line);
        Assert.True(order.Submit(Creator, Now).IsSuccess);
        Assert.True(order.Approve(Number, Creator, Now).IsSuccess); // its creator approves
        return order;
    }

    [Fact]
    public void Line_amounts_round_gross_discount_and_tax_each_to_the_currency_and_keep_an_exact_net_unit_price()
    {
        var egp = LineAmounts.Calculate(3m, Price(10.333m, discount: 10m, tax: 14m), currencyDecimals: 2, line: 1).Value;
        Assert.Equal((31.00m, 3.10m, 27.90m, 3.91m, 31.81m, 9.2997m), (egp.Gross, egp.Discount, egp.Net, egp.Tax, egp.Total, egp.NetUnitPrice));

        var kwd = LineAmounts.Calculate(3m, Price(10.3335m, tax: 5m), Amounts.CurrencyDecimals("KWD"), line: 1).Value;
        Assert.Equal((31.001m, 1.550m), (kwd.Gross, kwd.Tax));
    }

    [Fact]
    public void Non_recoverable_tax_and_prices_out_of_range_are_refused_not_ignored()
    {
        Assert.Equal("Purchasing.Line.NonRecoverableTaxNotSupported",
            LineAmounts.Calculate(1m, Price(10m, tax: 14m, treatment: TaxTreatment.NonRecoverable), 2, 1).Error!.Code);
        Assert.Equal("Purchasing.Line.DiscountInvalid", LineAmounts.Calculate(1m, Price(10m, discount: 101m), 2, 1).Error!.Code);
        Assert.Equal("Purchasing.Line.QuantityMustBeWhole", Pieces().CheckQuantity(1.5m, 1).Error!.Code);
    }

    [Fact]
    public void An_order_is_approved_by_its_creator_numbered_and_received_in_parts_but_never_beyond_what_was_ordered()
    {
        var order = ApprovedOrder(10m, 10m);
        Assert.Equal((PurchaseOrderStatus.Approved, "PO-B-2026-000001", Creator), (order.Status, order.Number, order.ApprovedByUserId));
        var line = order.Lines[0];

        Assert.True(order.Receive([new ReceiptLineInput(line.Id, 4m, Location)], "GRN-1", Creator, Now).IsSuccess);
        Assert.Equal((PurchaseOrderStatus.PartiallyReceived, 6m), (order.Status, line.RemainingQuantity));
        Assert.Equal("Purchasing.PurchaseOrder.OverReceipt", order.Receive([new ReceiptLineInput(line.Id, 6.0001m, Location)], "GRN-2", Creator, Now).Error!.Code);
        Assert.Equal("Purchasing.PurchaseOrder.NotCancellable", order.Cancel(null, Creator, Now).Error!.Code);

        Assert.True(order.Receive([new ReceiptLineInput(line.Id, 6m, Location)], "GRN-2", Creator, Now).IsSuccess);
        Assert.Equal(PurchaseOrderStatus.Received, order.Status);
        Assert.Equal("Purchasing.PurchaseOrder.NotReceivable", order.Receive([new ReceiptLineInput(line.Id, 1m, Location)], "GRN-3", Creator, Now).Error!.Code);
        Assert.Equal("Purchasing.PurchaseOrder.NotClosable", order.Close("short", Creator, Now).Error!.Code);
    }

    [Fact]
    public void An_invoice_with_another_net_price_needs_approval_and_records_its_variance_without_exceeding_receipts()
    {
        var order = ApprovedOrder(10m, 10m, tax: 14m);
        var line = order.Lines[0];
        order.Receive([new ReceiptLineInput(line.Id, 10m, Location)], "GRN-1", Creator, Now);

        var invoice = SupplierInvoice.CreateDraft(order, new InvoiceTerms(" inv-00/12 ", new DateOnly(2026, 3, 1), null, null),
            [new InvoiceLineInput(line.Id, 6m, Price(9m, tax: 14m))], Creator, Now).Value;
        Assert.Equal((true, -6m, 54m, 7.56m, "INV0012", new DateOnly(2026, 3, 31)),
            (invoice.RequiresApproval, invoice.PriceVarianceAmount, invoice.NetAmount, invoice.TaxAmount, invoice.SupplierInvoiceNumberKey, invoice.DueDate));
        Assert.Equal("Purchasing.SupplierInvoice.ApprovalRequired", invoice.CanPost(byApproval: false).Error!.Code);
        Assert.True(invoice.Submit(Creator, Now).IsSuccess);
        Assert.True(invoice.CanPost(byApproval: true).IsSuccess); // whoever entered it may approve it

        Assert.True(order.RecordInvoice(invoice.Matches(), "SIN-1", Creator, Now).IsSuccess);
        Assert.Equal(4m, line.InvoiceableQuantity);
        Assert.Equal("Purchasing.PurchaseOrder.InvoiceExceedsReceipts",
            order.RecordInvoice([new InvoiceMatch(line.Id, 4.5m, 45m, 0m)], "SIN-2", Creator, Now).Error!.Code);
    }

    [Fact]
    public void A_return_takes_uninvoiced_goods_first_and_claims_a_credit_at_the_invoiced_price_for_the_rest()
    {
        var order = ApprovedOrder(10m, 10m, tax: 14m);
        var line = order.Lines[0];
        order.Receive([new ReceiptLineInput(line.Id, 10m, Location)], "GRN-1", Creator, Now);
        order.RecordInvoice([new InvoiceMatch(line.Id, 6m, 54m, 7.56m)], "SIN-1", Creator, Now);

        var split = Assert.Single(order.RecordReturn([new ReturnedQuantity(line.Id, 5m)], "SRN-1", Creator, Now).Value);
        Assert.Equal((4m, 1m, 9m, 1.26m), (split.UninvoicedQuantity, split.InvoicedQuantity, split.CreditClaimNet, split.CreditClaimTax));
        Assert.Equal((0m, 5m, 1m), (line.InvoiceableQuantity, line.ReturnedQuantity, line.ReturnedInvoicedQuantity));
        Assert.Equal(PurchaseOrderStatus.Received, order.Status); // returns never reopen an order
        Assert.Equal("Purchasing.PurchaseOrder.ReturnExceedsReceipts",
            order.RecordReturn([new ReturnedQuantity(line.Id, 5.5m)], "SRN-2", Creator, Now).Error!.Code);
    }

    [Fact]
    public void Orders_never_take_more_of_a_request_line_than_was_approved_and_a_cancelled_order_gives_it_back()
    {
        var request = Saved(PurchaseRequest.CreateDraft(Warehouse, Branch, requiresApproval: true, null, null,
            [new RequestLineInput(Pieces(), 10m, null)], Creator, Now).Value);
        var line = Saved(request.Lines[0]);
        Assert.Equal("Purchasing.PurchaseRequest.NotOrderable", request.ChangeOrdered([new RequestAllocation(line.Id, 1m, 1)]).Error!.Code);
        request.Submit(Creator, Now);
        Assert.True(request.Approve(new DocumentNumber("PR-B-2026-000001", 2026, 1), Creator, Now).IsSuccess);

        Assert.True(request.ChangeOrdered([new RequestAllocation(line.Id, 6m, 1)]).IsSuccess);
        Assert.Equal("Purchasing.PurchaseRequest.ExceedsRequest", request.ChangeOrdered([new RequestAllocation(line.Id, 5m, 1)]).Error!.Code);
        Assert.True(request.ChangeOrdered([new RequestAllocation(line.Id, 4m, 1)]).IsSuccess);
        Assert.Equal(PurchaseRequestStatus.Ordered, request.Status);
        Assert.True(request.ChangeOrdered([new RequestAllocation(line.Id, -4m, 1)]).IsSuccess);
        Assert.Equal((PurchaseRequestStatus.Approved, 4m), (request.Status, line.RemainingQuantity));
        Assert.Equal("Purchasing.PurchaseRequest.NotCancellable", request.Cancel(Creator, Now).Error!.Code);
    }

    [Theory]
    [InlineData("شركة الأمل للتجارة", "الامل للتجاره")]
    [InlineData("Acme Trading Co.", "ACME  trading")]
    [InlineData("مؤسسة النُّور", "النور")]
    public void Names_that_differ_only_in_spelling_details_and_legal_form_share_their_duplicate_key(string first, string second) =>
        Assert.Equal(SupplierNames.Key(first), SupplierNames.Key(second));

    [Fact]
    public void Registration_numbers_are_kept_as_letters_and_digits_and_an_egyptian_tax_number_has_nine_digits()
    {
        Assert.Equal("123456789", RegistrationNumbers.Normalize(" 123-456-789 "));
        Assert.Equal("123456789", RegistrationNumbers.Normalize("١٢٣ ٤٥٦ ٧٨٩"));
        var details = new SupplierDetails("مورد", null, "eg", "12345678", null, null, 0, null, null, null, [], []);
        Assert.Equal("Purchasing.Supplier.EgyptianTaxNumberInvalid", Supplier.Create("SUP-1", details).Error!.Code);
        Assert.Equal("Purchasing.Supplier.CommercialRegistryRequired",
            Supplier.Create("SUP-1", details with { TaxRegistrationNumber = "123456789", CommercialRegistrationNumber = "55" }).Error!.Code);
        var supplier = Supplier.Create("sup-1", details with { TaxRegistrationNumber = "123-456-789" }).Value;
        Assert.Equal(("SUP-1", "EG", "123456789"), (supplier.Code, supplier.CountryCode, supplier.TaxRegistrationNumber));
    }

    [Fact]
    public void A_supplier_return_is_approved_by_its_creator_and_records_who_approved_it()
    {
        var order = ApprovedOrder(10m, 10m);
        var receipt = GoodsReceipt.Create(order, new DocumentNumber("GRN-B-2026-000001", 2026, 1),
            [new ReceiptLineInput(order.Lines[0].Id, 10m, Location)], null, null, Creator, Now);
        Saved(receipt.Lines[0]);
        var supplierReturn = Saved(SupplierReturn.CreateDraft(receipt, "EGP", "damaged", null,
            [new ReturnLineInput(receipt.Lines[0].Id, 2m, null)], Creator, Now).Value);
        Assert.Equal("Purchasing.SupplierReturn.NotSubmitted", supplierReturn.CanApprove().Error!.Code);
        supplierReturn.Submit(Creator, Now);

        supplierReturn.MarkPosted(new DocumentNumber("SRN-B-2026-000001", 2026, 1), [new ReturnSplit(2m, 0m, 0m, 0m)],
            Guid.NewGuid(), "PRT-B-2026-000001", Creator, Now);
        Assert.Equal((SupplierReturnStatus.Posted, Creator, 0m, Location),
            (supplierReturn.Status, supplierReturn.ApprovedByUserId, supplierReturn.CreditClaimTotalAmount, supplierReturn.Lines[0].LocationId));
    }
}
