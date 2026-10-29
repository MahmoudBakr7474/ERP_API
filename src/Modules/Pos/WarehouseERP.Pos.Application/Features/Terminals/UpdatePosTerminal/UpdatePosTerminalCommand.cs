namespace WarehouseERP.Pos.Application.Features.Terminals.UpdatePosTerminal;

/// <summary>Changes what may change on a terminal; its code and warehouse are fixed.</summary>
/// <param name="SellingLocationId">A location of its warehouse; null takes the DEFAULT location.</param>
public sealed record UpdatePosTerminalCommand(
    Guid TerminalId, string Name, Guid? SellingLocationId, bool AcceptsCash, bool AcceptsCard, string? ReceiptHeader,
    string? ReceiptFooter, string RowVersion);
