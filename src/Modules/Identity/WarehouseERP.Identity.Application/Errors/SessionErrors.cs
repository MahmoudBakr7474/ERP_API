using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Errors;

// Application rules about sessions; an ended session itself is SessionDomainErrors.
public static class SessionErrors
{
    public static readonly Error NotFound = Error.NotFound("Identity.Session.NotFound", "The session does not exist.");

    public static readonly Error AccessDenied = Error.Forbidden(
        "Identity.Session.AccessDenied", "You can only see and end your own sessions.");
}
