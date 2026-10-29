using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Application.Contracts.Customers;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Customers.FindSimilarCustomers;

// A warning, never a refusal: two customers may share a name or a phone. Names match on their normalized keys, mobiles
// on their digits (CustomerNames).
public sealed class FindSimilarCustomersHandler(ICustomerReadStore customers)
    : IQueryHandler<FindSimilarCustomersQuery, IReadOnlyList<SimilarCustomerResponse>>
{
    public async Task<Result<IReadOnlyList<SimilarCustomerResponse>>> HandleAsync(FindSimilarCustomersQuery query, CancellationToken ct)
    {
        var keys = new List<string> { CustomerNames.Key(query.NameAr) };
        if (!string.IsNullOrWhiteSpace(query.NameEn))
            keys.Add(CustomerNames.Key(query.NameEn));
        return Result.Success(await customers.FindSimilarAsync(
            keys.Distinct().ToList(), CustomerNames.PhoneKey(query.Mobile), query.ExceptCustomerId, ct));
    }
}
