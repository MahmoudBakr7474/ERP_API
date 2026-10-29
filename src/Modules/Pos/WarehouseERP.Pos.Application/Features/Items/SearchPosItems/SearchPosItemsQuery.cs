namespace WarehouseERP.Pos.Application.Features.Items.SearchPosItems;

/// <param name="Search">Part of a SKU or name, or an exact barcode.</param>
/// <param name="Take">At most this many products (default 20, at most 50); each comes with all its active units.</param>
public sealed record SearchPosItemsQuery(Guid TerminalId, string Search, int? Take);
