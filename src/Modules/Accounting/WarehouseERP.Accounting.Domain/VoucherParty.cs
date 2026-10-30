using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Domain;

/// <summary>A voucher's customer or supplier and the control account its line goes to (the mapped receivables or payables).</summary>
public sealed record VoucherParty(PartyType Type, Guid PartyId, Guid ControlAccountId);
