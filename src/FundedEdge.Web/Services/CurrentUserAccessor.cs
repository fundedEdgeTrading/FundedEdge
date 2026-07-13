using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using FundedEdge.Application.Abstractions;

namespace FundedEdge.Web.Services;

public class CurrentUserAccessor(AuthenticationStateProvider authStateProvider) : ICurrentUserAccessor
{
    public async Task<string?> GetUserIdAsync()
    {
        if (CurrentUserContext.OverrideUserId is not null)
        {
            return CurrentUserContext.OverrideUserId;
        }

        var state = await authStateProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated == true
            ? state.User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
    }
}
