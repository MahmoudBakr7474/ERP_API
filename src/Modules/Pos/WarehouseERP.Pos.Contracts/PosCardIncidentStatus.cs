namespace WarehouseERP.Pos.Contracts;

/// <summary>Where a card incident (a card approved for a checkout POS could not record) stands.</summary>
public enum PosCardIncidentStatus : byte
{
    /// <summary>The card was charged but no sale was recorded: reverse it on the card terminal or settle it otherwise.</summary>
    ReversalRequired = 1,

    /// <summary>A retry with the same Idempotency-Key recorded the sale after all; nothing is owed.</summary>
    SaleRecorded = 2,

    /// <summary>A shift manager confirmed it was reversed or settled, with a note.</summary>
    Resolved = 3,
}
