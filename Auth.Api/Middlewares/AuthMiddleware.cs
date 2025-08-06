using Microsoft.AspNetCore.Identity;
using Auth.Business;
using Auth.Data.Entities;
using OpenIddict.Abstractions;

namespace Auth.Api.Middlewares;

public class AuthMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        RequestEnvironment environment,
        UserManager<ApplicationUser> userManager,
        AccountsService accountsService)
    {
        var userId = context.User.GetClaim(OpenIddictConstants.Claims.Subject);

        if (userId != null)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user != null)
            {
                environment.AuthUser = user;
            }
        }

        await next(context);
    }
}
