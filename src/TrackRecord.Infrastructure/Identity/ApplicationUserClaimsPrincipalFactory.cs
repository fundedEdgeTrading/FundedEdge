using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace TrackRecord.Infrastructure.Identity;

/// <summary>Añade el claim "DisplayName" al ClaimsPrincipal, para poder mostrarlo en la sidebar sin ir a BD.</summary>
public class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser>(userManager, optionsAccessor)
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
