using Auth.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Api.Controllers;

public class UserinfoController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserinfoController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    [Produces("application/json")]
    public async Task<IActionResult> Userinfo()
    {
        var user = await _userManager.FindByIdAsync(User.GetClaim(Claims.Subject));

        if (user is null)
        {
            return Challenge(authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "The specified access token is bound to an account that no longer exists.",
                }));
        }

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Claims.Subject] = await _userManager.GetUserIdAsync(user),
        };

        if (User.HasScope(Scopes.Email))
        {
            claims[Claims.Email] = await _userManager.GetEmailAsync(user);
            claims[Claims.EmailVerified] = await _userManager.IsEmailConfirmedAsync(user);
        }

        if (User.HasScope(Scopes.Phone))
        {
            claims[Claims.PhoneNumber] = await _userManager.GetPhoneNumberAsync(user);
            claims[Claims.PhoneNumberVerified] = await _userManager.IsPhoneNumberConfirmedAsync(user);
        }

        if (User.HasScope(Scopes.Roles))
        {
            claims[Claims.Role] = await _userManager.GetRolesAsync(user);
        }

        return Ok(claims);
    }
}
