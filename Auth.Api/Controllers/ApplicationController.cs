using Auth.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;

namespace Auth.Api.Controllers;

// ViewModels/AuthorizedAppViewModel.cs
public class AuthorizedAppViewModel
{
    public string AuthorizationId { get; set; }
    public string ApplicationName { get; set; }
    public string LogoUrl { get; set; }
    public string[] Scopes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string LastUsedGeo { get; set; }
    public string ClientId { get; set; }

    // Human-readable scope descriptions
    public Dictionary<string, string> ScopeDescriptions { get; set; } = new();
}

// ViewModels/AuthorizedAppsIndexViewModel.cs
public class AuthorizedAppsIndexViewModel
{
    public List<AuthorizedAppViewModel> Applications { get; set; } = new();
    public bool HasApplications => Applications?.Any() == true;
}
//bob217@huy.test.mail.test.ru
public class ApplicationController(UserManager<ApplicationUser> userManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictApplicationManager applicationManager) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Userinfo()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var viewModel = new AuthorizedAppsIndexViewModel();

        await foreach (var authorization in authorizationManager.ListAsync())
        {
            var status = await authorizationManager.GetStatusAsync(authorization);
            var subject = await authorizationManager.GetSubjectAsync(authorization);


            if (status != "valid" || subject != userId) continue;

            var applicationId = await authorizationManager.GetApplicationIdAsync(authorization);
            if (applicationId == null) continue;

            var application = await applicationManager.FindByIdAsync(applicationId);
            if (application == null) continue;

            var scopes = await authorizationManager.GetScopesAsync(authorization);

            var appViewModel = new AuthorizedAppViewModel();
            appViewModel.ClientId = applicationId;
            appViewModel.ApplicationName = await applicationManager.GetDisplayNameAsync(application) ?? "Unknown Application";
            //LogoUrl = await applicationManager.GetPropertyAsync(application, "logo_uri") as string,
            appViewModel.Scopes = scopes.ToArray();
            appViewModel.CreatedAt = (await authorizationManager.GetCreationDateAsync(authorization) ?? DateTimeOffset.Now).LocalDateTime;
            appViewModel.AuthorizationId = await authorizationManager.GetIdAsync(authorization);

            // Получаем дополнительные свойства если они есть
            var properties = await authorizationManager.GetPropertiesAsync(authorization);
            if (properties != null)
            {
                appViewModel.UpdatedAt = properties.TryGetValue("updated_at", out var updatedAt) &&
                    DateTime.TryParse(updatedAt.ToString(), out var updatedDate) ? updatedDate : null;

                appViewModel.LastUsedGeo = properties.TryGetValue("last_used_geo", out var geo) ? geo.ToString() : null;
            }

            appViewModel.ScopeDescriptions = GetScopeDescriptions(appViewModel.Scopes);
            viewModel.Applications.Add(appViewModel);
            viewModel.Applications.Add(appViewModel);
        }

        return View(viewModel);
    }

    private Dictionary<string, string> GetScopeDescriptions(string[] scopes)
    {
        var scopeDictionary = new Dictionary<string, string>
        {
            ["openid"] = "Доступ к вашей базовой информации",
            ["profile"] = "Просмотр информации вашего профиля",
            ["email"] = "Просмотр вашего адреса электронной почты",
            ["offline_access"] = "Доступ к вашему аккаунту в ваше отсутствие",
            ["api"] = "Доступ к API",
            ["read"] = "Просмотр данных",
            ["write"] = "Изменение данных",
            ["delete"] = "Удаление данных"
        };

        var descriptions = new Dictionary<string, string>();
        foreach (var scope in scopes)
        {
            descriptions[scope] = scopeDictionary.TryGetValue(scope, out var description)
                ? description
                : $"Разрешение: {scope}";
        }

        return descriptions;
    }
}
