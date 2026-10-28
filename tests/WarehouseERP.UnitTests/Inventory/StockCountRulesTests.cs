using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.UnitTests.Inventory;

// A count's life: its scope, the freeze from start to completion or cancellation, counted quantities against the
// snapshot, and approval by someone other than its creator.
public sealed class StockCountRulesTests
{
    private static readonly Guid Warehouse = Guid.NewGuid();
    private static readonly Guid Branch = Guid.NewGuid();
    private static readonly Guid Shelf = Guid.NewGuid();
    private static readonly Guid OtherShelf = Guid.NewGuid();
    private static readonly Guid Pens = Guid.NewGuid();
    private static readonly Guid Paper = Guid.NewGuid();
    private static readonly Guid Creator = Guid.NewGuid();
    private static readonly Guid Approver = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    // EF Core sets the creator on save; the rules tests set it the same way.
    private static StockCount Draft(params Guid[] locations)
    {
        var count = StockCount.Create(Warehouse, Branch, locations, " yearly ", Creator, Now).Value;
        typeof(StockCount).GetProperty(nameof(StockCount.CreatedByUserId))!.SetValue(count, Creator);
        return count;
    }

    private static StockCount Counting(params Guid[] locations)
    {
        var count = Draft(locations);
        count.Start([new CountSnapshotLine(Pens, Shelf, 10m), new CountSnapshotLine(Paper, Shelf, 4m)], Creator, Now);
        return count;
    }

    private static CountEntry Counted(Guid product, decimal quantity, Guid? location = null, decimal? unitCost = null) =>
        new(product, location ?? Shelf, quantity, unitCost, BaseUnitAllowsFraction: false);

    [Fact]
    public void No_locations_means_the_whole_warehouse_otherwise_only_the_listed_ones()
    {
        var whole = Draft();
        Assert.True(whole.IsWholeWarehouse);
        Assert.Null(Assert.Single(whole.Scopes).LocationId);
        Assert.True(whole.Covers(Guid.NewGuid()));

        var some = Draft(Shelf, Shelf, OtherShelf);
        Assert.False(some.IsWholeWarehouse);
        Assert.Equal([Shelf, OtherShelf], some.Scopes.Select(s => s.LocationId!.Value));
        Assert.True(some.Covers(OtherShelf));
        Assert.False(some.Covers(Guid.NewGuid()));
        Assert.Equal("yearly", some.Notes);

        var tooMany = Enumerable.Range(0, StockCount.MaxLocations + 1).Select(_ => Guid.NewGuid()).ToList();
        Assert.Equal("Inventory.StockCount.TooManyLocations",
            StockCount.Create(Warehouse, Branch, tooMany, null, Creator, Now).Error?.Code);
    }

    [Fact]
    public void Starting_freezes_the_scope_and_keeps_what_is_expected()
    {
        var count = Draft(Shelf);
        Assert.False(Assert.Single(count.Scopes).IsActive);
        Assert.Equal("Inventory.StockCount.NotCounting", count.EnterCounts([Counted(Pens, 1m)]).Error?.Code);

        Assert.True(count.Start([new CountSnapshotLine(Pens, Shelf, 10m)], Creator, Now).IsSuccess);

        Assert.True(Assert.Single(count.Scopes).IsActive);
        Assert.Equal((StockCountStatus.Counting, Now, (Guid?)Creator), (count.Status, count.StartedAtUtc, count.StartedByUserId));
        var line = Assert.Single(count.Lines);
        Assert.Equal((10m, (decimal?)null, 0m), (line.ExpectedQuantity, line.CountedQuantity, line.Variance));
        Assert.Equal("Inventory.StockCount.NotDraft", count.Start([], Creator, Now).Error?.Code);
    }

    [Fact]
    public void Counted_quantities_give_the_differences_and_a_find_where_nothing_was_expected_expects_zero()
    {
        var count = Counting(Shelf);

        Assert.Equal("Inventory.StockCount.OutOfScope", count.EnterCounts([Counted(Pens, 1m, OtherShelf)]).Error?.Code);
        Assert.Equal("Inventory.StockCount.QuantityMustBeWhole", count.EnterCounts([Counted(Pens, 1.5m)]).Error?.Code);
        Assert.All(count.Lines, l => Assert.Null(l.CountedQuantity)); // a refused batch changes nothing

        var found = Guid.NewGuid();
        Assert.True(count.EnterCounts([Counted(Pens, 8m), Counted(Paper, 4m), Counted(found, 2m, unitCost: 3.5m)]).IsSuccess);
        Assert.True(count.EnterCounts([Counted(Pens, 7m)]).IsSuccess); // a recount replaces the quantity

        var differences = count.Differences.OrderBy(d => d.Variance).ToList();
        Assert.Equal([(Pens, -3m, (decimal?)null), (found, 2m, (decimal?)3.5m)],
            differences.Select(d => (d.ProductId, d.Variance, d.UnitCost)));
        Assert.Equal(0m, count.Lines.Single(l => l.ProductId == found).ExpectedQuantity);
    }

    [Fact]
    public void Submitting_needs_every_line_counted_and_a_rejection_sends_it_back_still_frozen()
    {
        var count = Counting();
        count.EnterCounts([Counted(Pens, 10m)]);
        Assert.Equal("Inventory.StockCount.NotFullyCounted", count.Submit(Creator, Now).Error?.Code);

        count.EnterCounts([Counted(Paper, 3m)]);
        Assert.True(count.Submit(Creator, Now).IsSuccess);
        Assert.Equal("Inventory.StockCount.NotCounting", count.EnterCounts([Counted(Paper, 4m)]).Error?.Code);

        Assert.True(count.Reject("recount paper", Approver, Now).IsSuccess);
        Assert.Equal(StockCountStatus.Counting, count.Status);
        Assert.True(Assert.Single(count.Scopes).IsActive);
        Assert.Equal("recount paper", count.Events[^1].Note);
    }

    [Fact]
    public void Approval_by_anyone_including_its_creator_completes_it_and_releases_the_freeze()
    {
        var count = Counting();
        count.EnterCounts([Counted(Pens, 10m), Counted(Paper, 4m)]);
        Assert.Equal("Inventory.StockCount.NotSubmitted", count.CanApprove().Error?.Code);
        Assert.Throws<InvalidOperationException>(() => count.Complete(null, Creator, Now));
        count.Submit(Creator, Now);
        Assert.True(count.CanApprove().IsSuccess);

        count.Complete(adjustmentNumber: null, Creator, Now);

        Assert.Equal((StockCountStatus.Completed, (Guid?)Creator, (DateTime?)Now),
            (count.Status, count.ApprovedByUserId, count.CompletedAtUtc));
        Assert.False(Assert.Single(count.Scopes).IsActive);
        Assert.Empty(count.Differences);
        Assert.Equal([StockCountAction.Created, StockCountAction.Started, StockCountAction.Submitted, StockCountAction.Approved,
            StockCountAction.Completed], count.Events.Select(e => e.Action));
        Assert.Equal("Inventory.StockCount.NotOpen", count.Cancel(Creator, Now).Error?.Code);
    }

    [Fact]
    public void Cancelling_releases_the_freeze_and_posts_nothing()
    {
        var count = Counting(Shelf, OtherShelf);

        Assert.True(count.Cancel(Creator, Now).IsSuccess);

        Assert.Equal(StockCountStatus.Cancelled, count.Status);
        Assert.All(count.Scopes, s => Assert.False(s.IsActive));
        Assert.Equal("Inventory.StockCount.NotOpen", count.Cancel(Creator, Now).Error?.Code);
    }

    [Fact]
    public void The_count_adjustment_carries_the_count_and_cannot_be_edited_or_reversed()
    {
        var count = Counting();
        var adjustment = StockDocument.CreateCountAdjustment(count,
            [new GeneratedLineInput(Pens, Guid.NewGuid(), Guid.NewGuid(), 3m, StockDirection.Out, null, new StockPlace(Warehouse, Shelf))],
            Approver, Now);

        Assert.Equal((StockDocumentType.CountAdjustment, StockDocumentStatus.Submitted, Warehouse, (Guid?)count.Id),
            (adjustment.DocumentType, adjustment.Status, adjustment.WarehouseId, adjustment.CountId));
        Assert.Equal("Inventory.StockDocument.LinkedLinesFixed", adjustment.ReplaceDraft(null, null, null, []).Error?.Code);
        adjustment.MarkPostedGenerated("CNT-1", 2026, 1, approvedBy: Approver, Approver, Now);
        Assert.Equal("Inventory.StockDocument.NotReversible", StockDocument.CreateReversal(adjustment, null, Creator, Now).Error?.Code);
        Assert.Throws<ArgumentException>(() => StockDocument.CreateCountAdjustment(count, [], Approver, Now));
    }
}
