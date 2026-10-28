using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.UnitTests.Inventory;

// V1 costing policy for reversing a receipt (ADR 0015, pending the accountant's approval): the reversed quantity leaves
// at the current average cost, like any stock going out. Reversing at the receipt's own cost would need a price
// variance account (General Ledger, not in V1) and can leave the valuation negative once part of the stock was issued.
public sealed class ReceiptReversalCostingTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    // Receipt A: 10 @ 1000, receipt B: 10 @ 1, then 9 issued at the 500.5 average: 11 left worth 5505.5.
    private static InventoryCostState AfterTwoReceiptsAndAnIssue()
    {
        var state = InventoryCostState.Start(Guid.NewGuid(), Now);
        state.Receive(10m, 10000m, Now);
        state.Receive(10m, 10m, Now);
        Assert.Equal(new CostChange(-9m, -4504.5m, 500.5m), state.Issue(9m, Now));
        Assert.Equal((11m, 5505.5m), (state.TotalQuantity, state.TotalValue));
        return state;
    }

    [Fact]
    public void A_reversed_receipt_leaves_at_the_current_average_and_quantity_and_value_stay_consistent()
    {
        var state = AfterTwoReceiptsAndAnIssue();

        var reversal = state.Issue(10m, Now); // receipt A reversed

        Assert.Equal(new CostChange(-10m, -5005m, 500.5m), reversal);
        Assert.Equal((1m, 500.5m, 500.5m), (state.TotalQuantity, state.TotalValue, state.AverageUnitCost));
    }

    [Fact]
    public void Reversing_at_the_receipts_own_value_would_leave_one_unit_worth_less_than_nothing()
    {
        var state = AfterTwoReceiptsAndAnIssue();

        var leftIfReversedAtOriginalValue = state.TotalValue - 10000m;

        Assert.True(state.TotalQuantity - 10m > 0);
        Assert.Equal(-4494.5m, leftIfReversedAtOriginalValue);
    }

    [Fact]
    public void Without_issues_in_between_both_ways_give_the_same_value()
    {
        var state = InventoryCostState.Start(Guid.NewGuid(), Now);
        state.Receive(10m, 1000m, Now);
        state.Receive(10m, 3000m, Now);

        Assert.Equal(-2000m, state.Issue(10m, Now).ValueDelta); // the average, 200, is no receipt's own cost (100 or 300)
        Assert.Equal((10m, 2000m), (state.TotalQuantity, state.TotalValue));
    }
}
