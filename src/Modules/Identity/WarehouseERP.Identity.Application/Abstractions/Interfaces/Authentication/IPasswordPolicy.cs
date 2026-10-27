using WarehouseERP.Identity.Application.Contracts.Authentication;

namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;

/// <summary>The password rules in force (Identity:Password), so clients can show them instead of hard-coding them.</summary>
public interface IPasswordPolicy
{
    PasswordPolicyResponse Current { get; }
}
