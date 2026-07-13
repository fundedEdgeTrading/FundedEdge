using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FundedEdge.Infrastructure.Identity;

/// <summary>
/// Añade los claims "DisplayName" y "AvatarUrl" al ClaimsPrincipal, para poder mostrarlos en la sidebar sin ir a BD.
/// La base con TRole incluye además los claims de rol (necesarios para [Authorize(Roles=...)]).
/// </summary>
public class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, optionsAccessor)
{
    public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var principal = await base.CreateAsync(user);
        if (principal.Identity is ClaimsIdentity identity)
        {
            if (!string.IsNullOrWhiteSpace(user.DisplayName))
            {
                identity.AddClaim(new Claim("DisplayName", user.DisplayName));
            }

            if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
            {
                identity.AddClaim(new Claim("AvatarUrl", user.AvatarUrl));
            }
        }

        return principal;
    }
}
