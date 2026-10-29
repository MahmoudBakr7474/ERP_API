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

    /// <summary>Goods received against a purchase order, posted only by Purchasing through IPurchaseStockPosting (PRC).</summary>
    PurchaseReceipt = 9,

    /// <summary>Received goods sent back to their supplier, posted only by Purchasing through IPurchaseStockPosting (PRT).</summary>
    PurchaseReturn = 10,

    /// <summary>Goods delivered to a customer out of a sales order's reservation, posted only by Sales through ISalesStockPosting (SDL).</summary>
    SalesDelivery = 11,

    /// <summary>Delivered goods a customer sent back fit for resale, posted only by Sales through ISalesStockPosting (SRT).</summary>
    SalesReturn = 12,

    /// <summary>Goods a POS checkout sold from unreserved stock, posted only by POS through IPosStockPosting (PSL).</summary>
    PosSale = 13,

    /// <summary>Goods a POS customer brought back fit for resale, posted only by POS through IPosStockPosting (PSR).</summary>
    PosReturn = 14,
}
