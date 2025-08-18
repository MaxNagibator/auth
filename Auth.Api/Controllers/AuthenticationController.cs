using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Client.AspNetCore;
using System.Security.Claims;

namespace Auth.Api.Controllers;

public class AuthenticationController : Controller
{
    [HttpGet("~/callback/login/{provider}")]
    [HttpPost("~/callback/login/{provider}")]
    [IgnoreAntiforgeryToken]
    public async Task<ActionResult> LogInCallback()
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);

        if (result.Principal is not ClaimsPrincipal { Identity.IsAuthenticated: true })
        {
            throw new InvalidOperationException("The external authorization data cannot be used for authentication.");
        }

        var identity = new ClaimsIdentity("ExternalLogin",
            ClaimTypes.Name,
            ClaimTypes.Role);

        identity.SetClaim(ClaimTypes.Email, result.Principal.GetClaim(ClaimTypes.Email))
            .SetClaim(ClaimTypes.Name, result.Principal.GetClaim(ClaimTypes.Name))
            .SetClaim(ClaimTypes.NameIdentifier, result.Principal.GetClaim(ClaimTypes.NameIdentifier))
            .SetClaim(OpenIddictConstants.Claims.Private.RegistrationId, result.Principal.GetClaim(OpenIddictConstants.Claims.Private.RegistrationId))
            .SetClaim(OpenIddictConstants.Claims.Private.ProviderName, result.Principal.GetClaim(OpenIddictConstants.Claims.Private.ProviderName));

        var properties = new AuthenticationProperties(result.Properties.Items)
        {
            RedirectUri = result.Properties.RedirectUri ?? "/",
        };

        properties.StoreTokens(result.Properties.GetTokens()
            .Where(token => token.Name is
                OpenIddictClientAspNetCoreConstants.Tokens.BackchannelAccessToken or
                OpenIddictClientAspNetCoreConstants.Tokens.RefreshToken));

        return SignIn(new(identity), properties);
    }
}
