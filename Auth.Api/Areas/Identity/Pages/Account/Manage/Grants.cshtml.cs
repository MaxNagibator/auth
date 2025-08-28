using Auth.Api.Data;
using Auth.Api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Api.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class GrantsModel(
    UserManager<ApplicationUser> userManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictApplicationManager applicationManager)
    : PageModel
{
    public IReadOnlyList<GrantItem> Grants { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public RevokeInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        var subject = await userManager.GetUserIdAsync(user);

        var items = new List<GrantItem>();
        var applications = await applicationManager.ListAsync().ToListAsync();

        foreach (var app in applications)
        {
            var appId = await applicationManager.GetIdAsync(app);

            if (string.IsNullOrEmpty(appId))
            {
                continue;
            }

            var permanent = await authorizationManager.FindAsync(subject,
                    appId,
                    Statuses.Valid,
                    AuthorizationTypes.Permanent,
                    ImmutableArray<string>.Empty)
                .ToListAsync();

            var adHoc = await authorizationManager.FindAsync(subject,
                    appId,
                    Statuses.Valid,
                    AuthorizationTypes.AdHoc,
                    ImmutableArray<string>.Empty)
                .ToListAsync();

            var appAuthorizations = permanent.Concat(adHoc).ToList();

            if (appAuthorizations.Count == 0)
            {
                continue;
            }

            var appName = await applicationManager.GetLocalizedDisplayNameAsync(app)
                          ?? await applicationManager.GetDisplayNameAsync(app)
                          ?? "Без понятия";

            var clientId = await applicationManager.GetClientIdAsync(app);

            foreach (var authorization in appAuthorizations)
            {
                var scopes = await authorizationManager.GetScopesAsync(authorization);

                var idTask = authorizationManager.GetIdAsync(authorization);
                var typeTask = authorizationManager.GetTypeAsync(authorization);
                var creationDateTask = authorizationManager.GetCreationDateAsync(authorization);
                var statusTask = authorizationManager.GetStatusAsync(authorization);

                items.Add(new()
                {
                    AuthorizationId = await idTask ?? string.Empty,
                    ApplicationName = appName,
                    ClientId = clientId,
                    AuthorizationType = await typeTask ?? string.Empty,
                    Scopes = scopes.IsDefaultOrEmpty ? string.Empty : string.Join(" ", scopes),
                    CreatedAt = await creationDateTask,
                    Status = await statusTask,
                });
            }
        }

        Grants = items;
        return Page();
    }

    public async Task<IActionResult> OnPostRevokeAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var user = await userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        var authorization = await authorizationManager.FindByIdAsync(Input.AuthorizationId);

        if (authorization is null)
        {
            StatusMessage = "Ошибка: Авторизация не найдена.";
            return RedirectToPage();
        }

        var subject = await authorizationManager.GetSubjectAsync(authorization);
        var currentUserId = await userManager.GetUserIdAsync(user);

        if (!string.Equals(subject, currentUserId, StringComparison.Ordinal))
        {
            return Forbid();
        }

        await authorizationManager.TryRevokeAsync(authorization);

        StatusMessage = "Доступ отозван.";
        return RedirectToPage();
    }

    public class GrantItem
    {
        public string AuthorizationId { get; set; } = string.Empty;
        public string? ClientId { get; set; }
        public string ApplicationName { get; set; } = string.Empty;
        public string AuthorizationType { get; set; } = string.Empty;
        public string Scopes { get; set; } = string.Empty;
        public DateTimeOffset? CreatedAt { get; set; }
        public string? Status { get; set; }
    }

    public class RevokeInput
    {
        [Required]
        public string AuthorizationId { get; set; } = string.Empty;
    }
}
