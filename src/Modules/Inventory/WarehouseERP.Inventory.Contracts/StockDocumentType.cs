namespace WarehouseERP.Inventory.Contracts;

/// <summary>What a stock document does; each type has its own number series.</summary>
public enum StockDocumentType : byte
{
    /// <summary>Stock a warehouse starts with, at a known cost (OPN).</summary>
    OpeningBalance = 1,

    /// <summary>Goods received into a warehouse, at a known cost (RCV).</summary>
    Receipt = 2,

    /// <summary>Goods issued out of a warehouse (ISS).</summary>
    Issue = 3,

    /// <summary>Increases and decreases with a reason: damage, loss, found stock, corrections (ADJ).</summary>
    Adjustment = 4,

    /// <summary>Undoes a posted document line by line; always approved (REV).</summary>
    Reversal = 5,

    /// <summary>A transfer leaving its source warehouse for its transit location (TRF); the transfer takes its number.</summary>
    TransferDispatch = 6,

    /// <summary>Part or all of a transfer arriving from its transit location in the destination warehouse (TRR).</summary>
    TransferReceipt = 7,

    /// <summary>The differences an approved stock count found (CNT).</summary>
    CountAdjustment = 8,
}
