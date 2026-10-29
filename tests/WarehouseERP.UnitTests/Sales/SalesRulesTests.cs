using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.UnitTests.Sales;

// Sales rules without a database: line amounts for each tax treatment and their rounding, why a document needs approval
// (configurable, nothing by default), an order delivered in parts but never beyond what was ordered, invoiced only up to
// what was delivered, returns that split into goods not invoiced yet and a credit for goods already invoiced, reducing,
// closing and cancelling, and the keys that catch duplicate customers.
public sealed class SalesRulesTests
{
    private static readonly Guid Warehouse = Guid.NewGuid();
    private static readonly Guid Branch = Guid.NewGuid();
    private static readonly Guid User = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DocumentNumber Number = new("SO-B-2026-000001", 2026, 1);

    private static ProductUnitFacts Pieces() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1m, false, false);

    private static LinePrice Price(
        decimal unitPrice, decimal discount = 0, SalesTaxTreatment treatment = SalesTaxTreatment.Standard, decimal tax = 14m) =>
        new(unitPrice, discount, treatment, tax);

    private static SalesLineInput Listed(decimal quantity, decimal unitPrice, decimal discount = 0) =>
        new(Pieces(), quantity, Price(unitPrice, discount), PriceSource.PriceList, unitPrice);

    private static SalesLineInput Manual(decimal quantity, decimal unitPrice, decimal? listPrice) =>
        new(Pieces(), quantity, Price(unitPrice), PriceSource.Manual, listPrice);

    // EF Core gives ids on save; these tests set them the same way.
    private static SalesOrder Draft(params SalesLineInput[] lines)
    {
        var terms = new OrderTerms(Guid.NewGuid(), new DateOnly(2026, 3, 1), null, null, 30, null, null);
        var order = SalesOrder.CreateDraft(Warehouse, Branch, "EGP", terms, lines, sourceQuotationId: null, User, Now).Value;
        typeof(SalesOrder).GetProperty("Id")!.SetValue(order, Guid.NewGuid());
        foreach (var line in order.Lines)
            typeof(PricedLine).GetProperty("Id")!.SetValue(line, Guid.NewGuid());
        return order;
    }

    private static SalesOrder Confirmed(params SalesLineInput[] lines)
    {
        var order = Draft(lines);
        Assert.True(order.ConfirmDirectly(Number, User, Now).IsSuccess);
        return order;
    }

    private static SalesSettings Settings(
        bool ordersRequireApproval = false, decimal? discountThreshold = null, bool manualPrice = false, bool belowList = false,
        CreditHoldPolicy creditHold = CreditHoldPolicy.BlockConfirmation)
    {
        var settings = (SalesSettings)Activator.CreateInstance(typeof(SalesSettings), nonPublic: true)!;
        Assert.True(settings.Change(false, ordersRequireApproval, discountThreshold, manualPrice, belowList, creditHold, true, 14m, 14).IsSuccess);
        return settings;
    }

    [Fact]
    public void Line_amounts_round_each_amount_to_the_currency_and_tax_only_standard_rated_lines()
    {
        var standard = LineAmounts.Calculate(3m, Price(10.333m, discount: 10m), currencyDecimals: 2, line: 1).Value;
        Assert.Equal((31.00m, 3.10m, 27.90m, 3.91m, 31.81m, 9.2997m),
            (standard.Gross, standard.Discount, standard.Net, standard.Tax, standard.Total, standard.NetUnitPrice));

        var zeroRated = LineAmounts.Calculate(2m, Price(10m, treatment: SalesTaxTreatment.ZeroRated, tax: 0m), 2, 1).Value;
        var exempt = LineAmounts.Calculate(2m, Price(10m, treatment: SalesTaxTreatment.Exempt, tax: 0m), 2, 1).Value;
        Assert.Equal((20m, 0m, 20m), (zeroRated.Net, zeroRated.Tax, zeroRated.Total));
        Assert.Equal((20m, 0m, 20m), (exempt.Net, exempt.Tax, exempt.Total));

        var kwd = LineAmounts.Calculate(3m, Price(10.3335m, tax: 5m), Amounts.CurrencyDecimals("KWD"), 1).Value;
        Assert.Equal((31.001m, 1.550m), (kwd.Gross, kwd.Tax));
    }

    [Fact]
    public void A_tax_or_price_the_line_cannot_carry_is_refused_never_dropped_silently()
    {
        Assert.Equal("Sales.Line.TaxRateInvalid", LineAmounts.Calculate(1m, Price(10m, tax: 0m), 2, 1).Error!.Code);
        Assert.Equal("Sales.Line.TaxRateInvalid", LineAmounts.Calculate(1m, Price(10m, treatment: SalesTaxTreatment.Exempt), 2, 1).Error!.Code);
        Assert.Equal("Sales.Line.TaxTreatmentNotSupported",
            LineAmounts.Calculate(1m, Price(10m, treatment: (SalesTaxTreatment)9), 2, 1).Error!.Code);
        Assert.Equal("Sales.Line.DiscountInvalid", LineAmounts.Calculate(1m, Price(10m, discount: 100.5m), 2, 1).Error!.Code);
        Assert.Equal("Sales.Line.UnitPriceInvalid", LineAmounts.Calculate(1m, Price(0m), 2, 1).Error!.Code);
        Assert.Equal("Sales.Line.QuantityMustBeWhole", Pieces().CheckQuantity(1.5m, 1).Error!.Code);
    }

    [Fact]
    public void Nothing_needs_approval_by_default_and_each_configured_rule_names_its_reason()
    {
        IReadOnlyCollection<PricedLine> lines = Draft(Listed(2m, 10m, discount: 10m), Manual(1m, 8m, listPrice: 10m)).Lines;

        Assert.Empty(Settings().ApprovalReasons(lines, isOrder: true, customerOnCreditHold: true)); // a hold refuses by default
        Assert.Empty(Settings(discountThreshold: 10m).ApprovalReasons(lines, isOrder: true, customerOnCreditHold: false)); // at it is fine
        Assert.Equal([ApprovalReason.DiscountAboveThreshold],
            Settings(discountThreshold: 5m).ApprovalReasons(lines, isOrder: true, customerOnCreditHold: false));
        Assert.Equal([ApprovalReason.Policy, ApprovalReason.PriceBelowList, ApprovalReason.ManualPrice, ApprovalReason.CreditHold],
            Settings(ordersRequireApproval: true, manualPrice: true, belowList: true, creditHold: CreditHoldPolicy.RequireApproval)
                .ApprovalReasons(lines, isOrder: true, customerOnCreditHold: true));
        Assert.Empty(Settings(creditHold: CreditHoldPolicy.RequireApproval).ApprovalReasons(lines, isOrder: false, customerOnCreditHold: true));
        Assert.Equal("Sales.Settings.DiscountThresholdInvalid",
            Settings().Change(false, false, 100m, false, false, CreditHoldPolicy.BlockConfirmation, true, 14m, 14).Error!.Code);
    }

    [Fact]
    public void An_order_is_delivered_in_parts_invoiced_up_to_its_deliveries_and_returns_split_into_uninvoiced_goods_and_a_credit()
    {
        var order = Confirmed(Listed(10m, 10m, discount: 10m));
        var line = order.Lines[0];
        Assert.Equal((SalesOrderStatus.Confirmed, "SO-B-2026-000001"), (order.Status, order.Number));

        Assert.Equal("Sales.Order.OverDelivery", order.RecordDelivery([new(line.Id, 6m), new(line.Id, 5m)], "DN-1", User, Now).Error!.Code);
        Assert.True(order.RecordDelivery([new(line.Id, 6m)], "DN-1", User, Now).IsSuccess);
        Assert.Equal((SalesOrderStatus.PartiallyDelivered, 4m, 6m), (order.Status, line.RemainingQuantity, line.InvoiceableQuantity));

        Assert.Equal("Sales.Order.InvoiceExceedsDeliveries", order.CanInvoice([new(line.Id, 7m)]).Error!.Code);
        Assert.True(order.RecordInvoice([new InvoiceMatch(line.Id, 4m, 36m, 5.04m)], 41.04m, "INV-1", User, Now).IsSuccess);
        Assert.Equal(2m, line.InvoiceableQuantity);

        // 3 come back: 2 were never invoiced, 1 was, credited at the line's average invoiced net and tax.
        var split = order.RecordReturn([new(line.Id, 3m)], "SR-1", User, Now).Value.Single();
        Assert.Equal((2m, 1m, 9m, 1.26m), (split.UninvoicedQuantity, split.InvoicedQuantity, split.CreditNetAmount, split.CreditTaxAmount));
        Assert.Equal((0m, 3m, 10.26m), (line.InvoiceableQuantity, line.ReturnableQuantity, order.CreditedAmount));
        Assert.Equal("Sales.Order.ReturnExceedsDeliveries", order.RecordReturn([new(line.Id, 4m)], "SR-2", User, Now).Error!.Code);
        Assert.Equal("Sales.Order.InvoiceExceedsDeliveries", order.CanInvoice([new(line.Id, 0.0001m)]).Error!.Code);
    }

    [Fact]
    public void Reducing_closing_and_cancelling_follow_what_was_delivered()
    {
        var order = Confirmed(Listed(5m, 10m));
        var line = order.Lines[0];
        Assert.Equal("Sales.Order.CancelInstead", order.ReduceLine(line.Id, 5m, "short", User, Now).Error!.Code);
        Assert.Equal("Sales.Order.NothingDeliveredToClose", order.Close("short", User, Now).Error!.Code);
        Assert.True(order.ReduceLine(line.Id, 2m, "short", User, Now).IsSuccess);
        Assert.Equal((3m, 2m), (line.RemainingQuantity, line.ClosedQuantity));

        Assert.True(order.RecordDelivery([new(line.Id, 1m)], "DN-1", User, Now).IsSuccess);
        Assert.Equal("Sales.Order.NotCancellable", order.Cancel(null, User, Now).Error!.Code);
        var closed = order.Close("no more", User, Now).Value.Single();
        Assert.Equal((line.Id, 2m), (closed.OrderLineId, closed.Quantity));
        Assert.Equal((SalesOrderStatus.Closed, 0m), (order.Status, line.RemainingQuantity));

        var reduced = Confirmed(Listed(4m, 10m));
        Assert.True(reduced.RecordDelivery([new(reduced.Lines[0].Id, 3m)], "DN-2", User, Now).IsSuccess);
        Assert.True(reduced.ReduceLine(reduced.Lines[0].Id, 1m, "short", User, Now).IsSuccess);
        Assert.Equal(SalesOrderStatus.Closed, reduced.Status); // nothing remains and something was taken off

        var cancelled = Confirmed(Listed(4m, 10m));
        Assert.Single(cancelled.Cancel("customer", User, Now).Value); // the lines whose reservation the caller releases
        Assert.Equal(SalesOrderStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public void Customer_keys_match_spellings_of_one_name_registration_or_phone()
    {
        Assert.Equal(CustomerNames.Key("شركة النور للتجارة"), CustomerNames.Key("النور  للتجاره"));
        Assert.Equal(CustomerNames.Key("Nour Trading Co."), CustomerNames.Key("NOUR trading"));
        Assert.Equal("01001234567", CustomerNames.PhoneKey("٠١٠٠ ١٢٣-٤٥٦٧"));
        Assert.Equal("123456789", RegistrationNumbers.Normalize(" 123-456-789 "));
        Assert.True(RegistrationNumbers.IsValidTaxNumber("EG", "123456789"));
        Assert.False(RegistrationNumbers.IsValidTaxNumber("EG", "12345"));
        Assert.True(RegistrationNumbers.IsValidTaxNumber("SA", "12345")); // only Egypt's format is checked in V1
    }
}
