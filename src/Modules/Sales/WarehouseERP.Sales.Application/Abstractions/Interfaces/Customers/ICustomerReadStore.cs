using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Contracts.Customers;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;

public interface ICustomerReadStore
{
    Task<CustomerDetailsResponse?> GetAsync(Guid customerId, CancellationToken ct);

    Task<PagedResult<CustomerSummaryResponse>> ListAsync(CustomerFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// Customers whose Arabic or English name has one of these keys (CustomerNames.Key), or whose mobile has this key, for
    /// duplicate warnings.
    /// </summary>
    Task<IReadOnlyList<SimilarCustomerResponse>> FindSimilarAsync(
        IReadOnlyCollection<string> nameKeys, string? mobileKey, Guid? exceptCustomerId, CancellationToken ct);

    /// <summary>
    /// The customer's open sales-order position as Sales knows it, from committed orders: not invoiced yet, delivered but
    /// not invoiced, invoiced, and credits pending; null when there is no such customer.
    /// </summary>
    Task<CustomerOrderPosition?> GetOrderPositionAsync(Guid customerId, CancellationToken ct);

    /// <summary>
    /// The customer's invoices posted and returns approved (with a credit) at or after the moment: what they bill, and their
    /// credits as negative amounts.
    /// </summary>
    Task<IReadOnlyList<CustomerReceivableDocument>> ListReceivableDocumentsAsync(Guid customerId, DateTime fromUtc, CancellationToken ct);
}
