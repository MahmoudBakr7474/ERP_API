namespace WarehouseERP.Accounting.Application.Features.Allocations.AllocateOpenItems;

/// <summary>Settles a debit open item (an invoice, a refund owed) against a credit open item (a receipt, credit or bill).</summary>
public sealed record AllocateOpenItemsCommand(Guid DebitItemId, Guid CreditItemId, decimal Amount);
