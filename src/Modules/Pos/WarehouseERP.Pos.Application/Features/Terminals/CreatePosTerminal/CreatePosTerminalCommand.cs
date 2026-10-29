namespace WarehouseERP.Pos.Application.Features.Terminals.CreatePosTerminal;

/// <summary>Registers a till in a physical warehouse that allows selling.</summary>
/// <param name="Code">Unique and fixed: upper-case letters, digits and dashes (lower case is upper-cased).</param>
/// <param name="SellingLocationId">A location of the warehouse; null takes its DEFAULT location.</param>
public sealed record CreatePosTerminalCommand(
    string Code, string Name, Guid WarehouseId, Guid? SellingLocationId, bool AcceptsCash, bool AcceptsCard, string? ReceiptHeader,
    string? ReceiptFooter);
