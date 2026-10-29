namespace WarehouseERP.Pos.Application.Features.Items.LookupPosItem;

/// <summary>What a terminal's scanner read.</summary>
public sealed record LookupPosItemQuery(Guid TerminalId, string Barcode);
