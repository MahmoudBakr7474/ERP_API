using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Application.Shared;

/// <summary>Where a journal posts: the period of its date (locked for the transaction) and the number it takes.</summary>
public sealed record PostingSlot(Guid FiscalPeriodId, DocumentNumber Number);
