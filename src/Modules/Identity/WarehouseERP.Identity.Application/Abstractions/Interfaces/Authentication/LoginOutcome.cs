namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;

public enum LoginOutcome
{
    Succeeded,
    UnknownUser,
    WrongPassword,
    LockedOut,
    Inactive,
}
