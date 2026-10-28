using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.UnitTests.Inventory;

// Company-wide moving weighted average: receipts recompute it, issues take value at it, rounding never leaves value
// without quantity.
public sealed class MovingAverageTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Receipts_recompute_the_average_and_issues_leave_at_it()
    {
        var state = InventoryCostState.Start(Guid.NewGuid(), Now);

        Assert.Equal(new CostChange(10m, 1000m, 100m), state.Receive(10m, 1000m, Now));
        Assert.Equal(new CostChange(10m, 2000m, 200m), state.Receive(10m, 2000m, Now));
        Assert.Equal((20m, 3000m, 150m), (state.TotalQuantity, state.TotalValue, state.AverageUnitCost));

        Assert.Equal(new CostChange(-5m, -750m, 150m), state.Issue(5m, Now));
        Assert.Equal((15m, 2250m, 150m), (state.TotalQuantity, state.TotalValue, state.AverageUnitCost));
    }

    [Fact]
    public void The_last_units_take_the_value_left_so_rounding_never_strands_value()
    {
        var state = InventoryCostState.Start(Guid.NewGuid(), Now);
        state.Receive(3m, 10m, Now);
        Assert.Equal(3.33333333m, state.AverageUnitCost);

        Assert.Equal(-3.3333m, state.Issue(1m, Now).ValueDelta);
        Assert.Equal(-6.6667m, state.Issue(2m, Now).ValueDelta);
        Assert.Equal((0m, 0m), (state.TotalQuantity, state.TotalValue));
        Assert.Equal(3.33333333m, state.AverageUnitCost); // kept for a later increase at the average
    }

    [Fact]
    public void An_increase_without_a_cost_comes_in_at_the_average()
    {
        var state = InventoryCostState.Start(Guid.NewGuid(), Now);
        state.Receive(4m, 50m, Now);

        var change = state.ReceiveAtAverage(2m, Now);

        Assert.Equal(new CostChange(2m, 25m, 12.5m), change);
        Assert.Equal((6m, 75m, 12.5m), (state.TotalQuantity, state.TotalValue, state.AverageUnitCost));
    }

    [Fact]
    public void Issuing_more_than_is_valued_is_a_ledger_inconsistency()
    {
        var state = InventoryCostState.Start(Guid.NewGuid(), Now);
        state.Receive(1m, 5m, Now);

        Assert.Throws<InvalidOperationException>(() => state.Issue(2m, Now));
        Assert.Equal((1m, 5m), (state.TotalQuantity, state.TotalValue));
    }
}
