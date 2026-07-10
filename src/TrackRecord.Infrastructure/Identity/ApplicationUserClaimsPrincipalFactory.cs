using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace TrackRecord.Infrastructure.Identity;

/// <summary>
/// Añade el claim "DisplayName" al ClaimsPrincipal, para poder mostrarlo en la sidebar sin ir a BD.
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
        if (!string.IsNullOrWhiteSpace(user.DisplayName) && principal.Identity is ClaimsIdentity identity)
        {
            identity.AddClaim(new Claim("DisplayName", user.DisplayName));
        }

        return principal;
    }
}
