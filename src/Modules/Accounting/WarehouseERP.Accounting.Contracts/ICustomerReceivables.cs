namespace WarehouseERP.Accounting.Contracts;

/// <summary>Read-only view of customers' receivables for other modules (Sales' credit exposure).</summary>
public interface ICustomerReceivables
{
    /// <summary>The customer's receivable, or null while accounting is not active yet.</summary>
    Task<CustomerReceivable?> GetAsync(Guid customerId, CancellationToken ct);
}
