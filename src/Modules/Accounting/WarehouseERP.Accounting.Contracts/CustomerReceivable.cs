namespace WarehouseERP.Accounting.Contracts;

/// <summary>
/// A customer's receivable as the ledger holds it. Balance: posted debits less credits on the customers' control accounts
/// (opening balances included). CutoverStartUtc: the moment automatic accounting starts; Sales documents posted from then on
/// reach the ledger through their journals. JournaledDocumentIds: the Sales invoices and returns of the customer already
/// journaled, so Sales can add the ones still waiting for their journal.
/// </summary>
public sealed record CustomerReceivable(decimal Balance, DateTime CutoverStartUtc, IReadOnlyCollection<Guid> JournaledDocumentIds);
