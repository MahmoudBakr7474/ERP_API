using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings.GetSourceReconciliation;

/// <summary>A clearing account's ledger balance (on its normal side) against what its module holds.</summary>
public sealed record ClearingAccountResponse(PostingRole Role, decimal LedgerBalance, decimal SourceValue, decimal Difference);
