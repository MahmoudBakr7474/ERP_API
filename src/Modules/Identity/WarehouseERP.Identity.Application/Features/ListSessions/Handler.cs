using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.Identity.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.ListSessions;

public sealed class ListSessionsHandler(ICurrentUser currentUser, ISessionRepository sessions)
    : IQueryHandler<ListSessionsQuery, IReadOnlyList<SessionItem>>
{
    public async Task<Result<IReadOnlyList<SessionItem>>> HandleAsync(ListSessionsQuery query, CancellationToken ct)
    {
        if (currentUser.UserId is not { } callerId)
            return IdentityApplicationErrors.UserNotFound;
        var userId = query.UserId ?? callerId;
        if (userId != callerId && !currentUser.HasPermission(IdentityPermissions.SessionsManage))
            return IdentityApplicationErrors.SessionAccessDenied;
        return Result.Success(await sessions.ListAsync(userId, ct));
    }
}
