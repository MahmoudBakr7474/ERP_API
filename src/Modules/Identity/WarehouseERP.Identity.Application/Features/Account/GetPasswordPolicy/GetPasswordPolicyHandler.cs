using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;
using WarehouseERP.Identity.Application.Contracts.Authentication;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Account.GetPasswordPolicy;

// For the change-password, create-user and reset-password forms: the same rules the server applies.
public sealed class GetPasswordPolicyHandler(IPasswordPolicy policy) : IQueryHandler<GetPasswordPolicyQuery, PasswordPolicyResponse>
{
    public Task<Result<PasswordPolicyResponse>> HandleAsync(GetPasswordPolicyQuery query, CancellationToken ct) =>
        Task.FromResult(Result.Success(policy.Current));
}
