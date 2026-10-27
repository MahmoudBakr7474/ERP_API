using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;
using WarehouseERP.Identity.Application.Contracts.Authentication;

namespace WarehouseERP.Identity.Infrastructure.Security;

// The ASP.NET Core Identity password options in force (Identity:Password), as the clients see them.
internal sealed class PasswordPolicy(IOptions<IdentityOptions> options) : IPasswordPolicy
{
    public PasswordPolicyResponse Current
    {
        get
        {
            var p = options.Value.Password;
            return new PasswordPolicyResponse(
                p.RequiredLength, p.RequiredUniqueChars, p.RequireDigit, p.RequireLowercase, p.RequireUppercase, p.RequireNonAlphanumeric);
        }
    }
}
