using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.UnitTests.Pos;

// POS rules without a database: how a sale is paid (cash tendered, change only from cash, cards never above the total,
// gross tendered, change and net cash kept apart), what a shift's drawer should hold (card amounts never count) and how it
// closes, returns that share a line's amounts out cumulatively so they always add up to the sale, refunds back the way the
// sale was paid, and the terminal's own invariants.
public sealed class PosRulesTests
{
    private static readonly Guid Warehouse = Guid.NewGuid();
    private static readonly Guid Cashier = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 10, 29, 9, 0, 0, DateTimeKind.Utc);

    // EF Core gives ids on save; these tests set them the same way.
    private static T WithId<T>(T entity, Type? declaring = null)
    {
        (declaring ?? typeof(T)).GetProperty("Id")!.SetValue(entity, Guid.NewGuid());
        return entity;
    }

    private static PosTerminal Terminal(bool cash = true, bool card = true, Guid? warehouse = null) =>
        WithId(PosTerminal.Create("cai-t1", "Till 1", Guid.NewGuid(), warehouse ?? Warehouse, Guid.NewGuid(), cash, card, null, null).Value,
            typeof(MasterEntity));

    private static PosShift Shift(PosTerminal terminal, decimal openingFloat = 100m) =>
        WithId(PosShift.Open(new DocumentNumber("CAI-T1-SH-2026-000001", 2026, 1), terminal, Cashier, openingFloat, 2, Now).Value);

    // A standard-rated line at 14% priced the way Sales does (no discount).
    private static PosSaleLineInput Line(decimal quantity, decimal unitPrice, decimal taxRate = 14m)
    {
        var net = Money.Round(quantity * unitPrice, 2);
        var tax = Money.Round(net * taxRate / 100m, 2);
        return new PosSaleLineInput(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1m, "SKU", "صنف", "PCS", quantity, unitPrice, 0m,
            taxRate == 0 ? PosTaxTreatment.Exempt : PosTaxTreatment.Standard, taxRate, net, 0m, net, tax, net + tax, unitPrice);
    }

    private static PosPaymentInput Cash(decimal amount) => new(PosPaymentMethod.Cash, amount, null, null);

    private static PosPaymentInput Card(decimal amount, string? reference = "AUTH-1", string? last4 = "4242") =>
        new(PosPaymentMethod.Card, amount, reference, last4);

    private static Result<PosSale> Sell(PosShift shift, PosTerminal terminal, PosSaleLineInput[] lines, params PosPaymentInput[] payments) =>
        PosSale.Create(new DocumentNumber("CAI-T1-2026-000001", 2026, 1), shift, terminal, PosSaleCustomer.WalkIn(null, null), "EGP", 2,
            lines, payments, null, Now);

    private static PosSale Posted(PosSale sale)
    {
        WithId(sale);
        foreach (var line in sale.Lines)
            WithId(line);
        sale.MarkPosted(Guid.NewGuid(), "PSL-CAI-2026-000001", sale.Lines.Select(_ => (Guid.NewGuid(), 5m)).ToList());
        return sale;
    }

    [Fact]
    public void Exact_cash_settles_the_total_and_leaves_it_all_in_the_drawer()
    {
        var terminal = Terminal();
        var shift = Shift(terminal);
        var sale = Sell(shift, terminal, [Line(2m, 50m)], Cash(114m)).Value;

        Assert.Equal((114m, 114m, 0m, 114m, 0m), (sale.TotalAmount, sale.CashTenderedAmount, sale.ChangeAmount, sale.CashReceivedAmount, sale.CardAmount));
        var payment = Assert.Single(sale.Payments);
        Assert.Equal((PosPaymentStatus.Settled, 114m, 114m, 0m), (payment.Status, payment.Amount, payment.TenderedAmount, payment.ChangeAmount));
        Assert.Equal((214m, 114m, 1), (shift.ExpectedCash, shift.SalesAmount, shift.SalesCount));
    }

    [Fact]
    public void Over_tendered_cash_gives_change_and_only_the_net_cash_counts_in_the_drawer()
    {
        var terminal = Terminal();
        var shift = Shift(terminal);
        var sale = Sell(shift, terminal, [Line(1m, 87.72m)], Cash(200m)).Value;

        Assert.Equal(100m, sale.TotalAmount);
        Assert.Equal((200m, 100m, 100m), (sale.CashTenderedAmount, sale.ChangeAmount, sale.CashReceivedAmount));
        Assert.Equal(200m, shift.ExpectedCash);
        Assert.Equal(100m, shift.CashSalesAmount);
    }

    [Fact]
    public void A_split_payment_takes_the_card_first_and_change_only_from_cash()
    {
        var terminal = Terminal();
        var shift = Shift(terminal);
        var sale = Sell(shift, terminal, [Line(1m, 100m)], Card(60m), Cash(100m)).Value;

        Assert.Equal(114m, sale.TotalAmount);
        Assert.Equal((60m, 100m, 46m, 54m), (sale.CardAmount, sale.CashTenderedAmount, sale.ChangeAmount, sale.CashReceivedAmount));
        Assert.Equal(sale.TotalAmount, sale.CashReceivedAmount + sale.CardAmount);
        var card = sale.Payments.Single(p => p.Method == PosPaymentMethod.Card);
        Assert.Equal((PosPaymentStatus.ExternallyConfirmed, "AUTH-1", "4242", 0m), (card.Status, card.Reference, card.CardLast4, card.ChangeAmount));
        Assert.Equal((154m, 60m), (shift.ExpectedCash, shift.CardSalesAmount));
    }

    [Fact]
    public void Payments_that_do_not_pay_exactly_the_total_are_refused()
    {
        var terminal = Terminal();
        PosSaleLineInput[] lines = [Line(1m, 100m)]; // 114.00

        Assert.Equal("Pos.Payment.CardExceedsTotal", Sell(Shift(terminal), terminal, lines, Card(120m)).Error!.Code);
        Assert.Equal("Pos.Payment.Underpaid", Sell(Shift(terminal), terminal, lines, Card(100m), Cash(10m)).Error!.Code);
        Assert.Equal("Pos.Payment.Underpaid", Sell(Shift(terminal), terminal, lines, Card(113.99m)).Error!.Code);
        Assert.Equal("Pos.Payment.CashNotNeeded", Sell(Shift(terminal), terminal, lines, Card(114m), Cash(20m)).Error!.Code);
        Assert.Equal("Pos.Payment.OneCashOnly", Sell(Shift(terminal), terminal, lines, Cash(100m), Cash(14m)).Error!.Code);
        Assert.Equal("Pos.Payment.Missing", Sell(Shift(terminal), terminal, lines).Error!.Code);
        Assert.Equal("Pos.Payment.AmountInvalid", Sell(Shift(terminal), terminal, lines, Cash(114.001m)).Error!.Code);
        Assert.True(Sell(Shift(terminal), terminal, lines, Card(114m)).IsSuccess);
    }

    [Fact]
    public void A_card_payment_needs_its_reference_never_more_than_its_last_four_digits_and_a_terminal_that_takes_cards()
    {
        var terminal = Terminal();
        PosSaleLineInput[] lines = [Line(1m, 100m)];

        Assert.Equal("Pos.Payment.CardReferenceRequired", Sell(Shift(terminal), terminal, lines, Card(114m, reference: " ")).Error!.Code);
        Assert.Equal("Pos.Payment.CardLast4Invalid", Sell(Shift(terminal), terminal, lines, Card(114m, last4: "4242424242424242")).Error!.Code);
        Assert.Equal("Pos.Payment.CardLast4Invalid", Sell(Shift(terminal), terminal, lines, Card(114m, last4: "42a2")).Error!.Code);

        var cashOnly = Terminal(card: false);
        Assert.Equal("Pos.Payment.MethodNotAccepted", Sell(Shift(cashOnly), cashOnly, lines, Card(114m)).Error!.Code);
    }

    [Fact]
    public void The_drawer_counts_float_cash_sales_cash_refunds_and_cash_in_and_out_but_never_cards()
    {
        var terminal = Terminal();
        var shift = Shift(terminal, openingFloat: 100m);
        Sell(shift, terminal, [Line(1m, 100m)], Cash(200m));   // +114 cash, 86 change
        Sell(shift, terminal, [Line(1m, 50m)], Card(57m));      // card only
        Assert.True(shift.MoveCash(PosCashMovementType.CashIn, 20m, "change", 2, Cashier, Now).IsSuccess);
        Assert.True(shift.MoveCash(PosCashMovementType.CashOut, 30m, "safe drop", 2, Cashier, Now).IsSuccess);
        Assert.True(shift.RecordReturn(cashRefund: 14m, cardRefund: 0m, total: 14m).IsSuccess);

        Assert.Equal(100m + 114m + 20m - 30m - 14m, shift.ExpectedCash);
        Assert.Equal((57m, 171m, 14m, 2, 1), (shift.CardSalesAmount, shift.SalesAmount, shift.RefundsAmount, shift.SalesCount, shift.ReturnsCount));
        Assert.Equal("Pos.Shift.DrawerShort", shift.MoveCash(PosCashMovementType.CashOut, 1000m, "too much", 2, Cashier, Now).Error!.Code);
        Assert.Equal("Pos.Shift.ReasonRequired", shift.MoveCash(PosCashMovementType.CashIn, 5m, " ", 2, Cashier, Now).Error!.Code);
        Assert.Equal("Pos.Shift.DrawerShort", shift.RecordReturn(cashRefund: 1000m, cardRefund: 0m, total: 1000m).Error!.Code);
    }

    [Fact]
    public void Closing_records_the_count_and_its_difference_which_needs_a_note_and_a_closed_shift_takes_nothing_more()
    {
        var terminal = Terminal();
        var shift = Shift(terminal, openingFloat: 100m);
        Sell(shift, terminal, [Line(1m, 100m)], Cash(114m));    // expected 214

        Assert.Equal("Pos.Shift.DifferenceNoteRequired", shift.Close(210m, null, forced: false, 2, Cashier, Now).Error!.Code);
        Assert.True(shift.Close(210m, "4 short", forced: false, 2, Cashier, Now).IsSuccess);
        Assert.Equal((PosShiftStatus.Closed, 214m, 210m, -4m), (shift.Status, shift.ExpectedCashAtClose, shift.CountedCash, shift.CashDifference));

        Assert.Equal("Pos.Shift.NotOpen", Sell(shift, terminal, [Line(1m, 10m)], Cash(11.40m)).Error!.Code);
        Assert.Equal("Pos.Shift.NotOpen", shift.MoveCash(PosCashMovementType.CashIn, 5m, "late", 2, Cashier, Now).Error!.Code);
        Assert.Equal("Pos.Shift.NotOpen", shift.Close(210m, "again", forced: false, 2, Cashier, Now).Error!.Code);
    }

    [Fact]
    public void A_manager_closing_someone_elses_shift_always_gives_a_note_and_the_close_shows_as_forced()
    {
        var shift = Shift(Terminal(), openingFloat: 50m);

        Assert.Equal("Pos.Shift.ForceCloseNoteRequired", shift.Close(50m, null, forced: true, 2, Guid.NewGuid(), Now).Error!.Code);
        Assert.True(shift.Close(50m, "cashier left", forced: true, 2, Guid.NewGuid(), Now).IsSuccess);
        Assert.True(shift.IsForceClosed);
        Assert.Equal(PosShiftAction.ForceClosed, shift.Events[^1].Action);
    }

    [Fact]
    public void Partial_returns_share_the_line_out_cumulatively_so_together_they_refund_exactly_what_was_sold()
    {
        var terminal = Terminal();
        var shift = Shift(terminal);
        var line = Line(3m, 3.3333m);                            // net 10.00, VAT 1.40
        var sale = Posted(Sell(shift, terminal, [line], Cash(11.40m)).Value);
        var lineId = sale.Lines[0].Id;

        var first = Assert.Single(sale.TakeBack([(lineId, 1m)]).Value);
        var second = Assert.Single(sale.TakeBack([(lineId, 1m)]).Value);
        var third = Assert.Single(sale.TakeBack([(lineId, 1m)]).Value);

        Assert.Equal((3.33m, 0.47m), (first.NetAmount, first.TaxAmount));
        Assert.Equal((3.34m, 0.46m), (second.NetAmount, second.TaxAmount));
        Assert.Equal((3.33m, 0.47m), (third.NetAmount, third.TaxAmount));
        Assert.Equal(11.40m, first.TotalAmount + second.TotalAmount + third.TotalAmount);
        Assert.Equal("Pos.Return.ExceedsSale", sale.TakeBack([(lineId, 0.5m)]).Error!.Code);
    }

    [Fact]
    public void Returns_never_take_back_more_than_a_line_sold_even_in_one_request()
    {
        var terminal = Terminal();
        var sale = Posted(Sell(Shift(terminal), terminal, [Line(2m, 10m)], Cash(22.80m)).Value);
        var lineId = sale.Lines[0].Id;

        Assert.Equal("Pos.Return.ExceedsSale", sale.TakeBack([(lineId, 1.5m), (lineId, 1m)]).Error!.Code);
        Assert.Equal("Pos.Return.SaleLineNotFound", sale.TakeBack([(Guid.NewGuid(), 1m)]).Error!.Code);
        Assert.Equal(0m, sale.Lines[0].ReturnedQuantity);
    }

    [Fact]
    public void A_return_refunds_exactly_its_worth_back_the_way_the_sale_was_paid()
    {
        var terminal = Terminal();
        var shift = Shift(terminal);
        // A failed return rolls back with its transaction; each attempt here starts from a fresh sale of 114.00 (card 60, cash 54).
        Result<PosReturn> Return(PosSale sale, PosReturnCondition condition, params PosPaymentInput[] refunds) =>
            PosReturn.Create(new DocumentNumber("CAI-T1-R-2026-000001", 2026, 1), sale, shift, terminal, "wrong size", null,
                sale.TakeBack([(sale.Lines[0].Id, 1m)]).Value.Select(s => new PosReturnLineInput(s, condition)).ToList(), refunds, Now);
        PosSale Fresh() => Posted(Sell(shift, terminal, [Line(2m, 50m)], Card(60m), Cash(54m)).Value);

        Assert.Equal("Pos.Refund.Mismatch", Return(Fresh(), PosReturnCondition.FitForResale, Cash(50m)).Error!.Code);
        Assert.Equal("Pos.Return.ConditionNotSupported", Return(Fresh(), PosReturnCondition.Damaged, Cash(57m)).Error!.Code);
        Assert.Equal("Pos.Return.ConditionNotSupported", Return(Fresh(), PosReturnCondition.Uninspected, Cash(57m)).Error!.Code);
        Assert.Equal("Pos.Refund.CashExceedsPaid", Return(Fresh(), PosReturnCondition.FitForResale, Cash(57m)).Error!.Code);
        Assert.Equal("Pos.Payment.CardReferenceRequired",
            Return(Fresh(), PosReturnCondition.FitForResale, Card(40m, reference: null), Cash(17m)).Error!.Code);

        var sale = Fresh();
        var refunded = Return(sale, PosReturnCondition.FitForResale, Card(40m, "RFD-9"), Cash(17m));
        Assert.True(refunded.IsSuccess);
        Assert.Equal((57m, 17m, 40m), (refunded.Value.TotalAmount, refunded.Value.CashRefundAmount, refunded.Value.CardRefundAmount));
        Assert.Equal((17m, 40m, 57m, 1), (sale.RefundedCashAmount, sale.RefundedCardAmount, sale.RefundedAmount, sale.ReturnCount));
        Assert.Equal(PosPaymentStatus.ExternallyConfirmed, refunded.Value.Refunds.Single(r => r.Method == PosPaymentMethod.Card).Status);
        Assert.Equal("Pos.Refund.CardExceedsPaid", sale.RecordRefunds(0m, 20.01m).Error!.Code);
    }

    [Fact]
    public void Goods_come_back_only_at_a_terminal_of_the_warehouse_they_were_sold_from()
    {
        var terminal = Terminal();
        var shift = Shift(terminal);
        var sale = Posted(Sell(shift, terminal, [Line(1m, 10m)], Cash(11.40m)).Value);
        var elsewhere = Terminal(warehouse: Guid.NewGuid());
        var shares = sale.TakeBack([(sale.Lines[0].Id, 1m)]).Value;

        var refused = PosReturn.Create(new DocumentNumber("X-R-2026-000001", 2026, 1), sale, Shift(elsewhere), elsewhere, "wrong", null,
            shares.Select(s => new PosReturnLineInput(s, PosReturnCondition.FitForResale)).ToList(), [Cash(11.40m)], Now);
        Assert.Equal("Pos.Return.OtherWarehouse", refused.Error!.Code);
    }

    [Fact]
    public void A_terminal_has_an_upper_case_code_a_name_and_takes_cash_or_cards()
    {
        Assert.Equal("CAI-T1", Terminal().Code);
        Assert.Equal("Pos.Terminal.CodeInvalid",
            PosTerminal.Create("till #1", "Till", Guid.NewGuid(), Warehouse, Guid.NewGuid(), true, true, null, null).Error!.Code);
        Assert.Equal("Pos.Terminal.NoPaymentMethod",
            PosTerminal.Create("T2", "Till", Guid.NewGuid(), Warehouse, Guid.NewGuid(), false, false, null, null).Error!.Code);
        Assert.Equal("Pos.Terminal.NameInvalid",
            PosTerminal.Create("T3", " ", Guid.NewGuid(), Warehouse, Guid.NewGuid(), true, false, null, null).Error!.Code);
    }

    [Fact]
    public void The_till_discount_limit_and_return_window_are_settings_within_their_ranges()
    {
        var settings = (PosSettings)Activator.CreateInstance(typeof(PosSettings), nonPublic: true)!;
        Assert.True(settings.Change(10m, 14).IsSuccess);
        Assert.Equal("Pos.Settings.MaxDiscountInvalid", settings.Change(100.5m, 14).Error!.Code);
        Assert.Equal("Pos.Settings.ReturnWindowInvalid", settings.Change(10m, -1).Error!.Code);
    }
}
