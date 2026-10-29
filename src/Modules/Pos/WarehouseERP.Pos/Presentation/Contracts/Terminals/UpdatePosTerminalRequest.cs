namespace WarehouseERP.Pos.Presentation.Contracts.Terminals;

/// <summary>What may change on a terminal; its code and warehouse are fixed.</summary>
public sealed record UpdatePosTerminalRequest(
    string Name, Guid? SellingLocationId, bool AcceptsCash, bool AcceptsCard, string? ReceiptHeader, string? ReceiptFooter, string RowVersion);
