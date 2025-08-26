#nullable disable

using Auth.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Auth.Api.Areas.Identity.Pages.Account.Manage;

public class GenerateRecoveryCodesModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<GenerateRecoveryCodesModel> _logger;

    public GenerateRecoveryCodesModel(
        UserManager<ApplicationUser> userManager,
        ILogger<GenerateRecoveryCodesModel> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [TempData]
    public string[] RecoveryCodes { get; set; }

    /// <summary>
    /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [TempData]
    public string StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return NotFound($"Не удалось загрузить пользователя с ID '{_userManager.GetUserId(User)}'.");
        }

        var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);

        if (!isTwoFactorEnabled)
        {
            throw new InvalidOperationException("Нельзя создать коды восстановления: 2FA не включена.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return NotFound($"Не удалось загрузить пользователя с ID '{_userManager.GetUserId(User)}'.");
        }

        var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        var userId = await _userManager.GetUserIdAsync(user);

        if (!isTwoFactorEnabled)
        {
            throw new InvalidOperationException("Нельзя создать коды восстановления: 2FA не включена.");
        }

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        RecoveryCodes = recoveryCodes.ToArray();

        _logger.LogInformation("User with ID '{UserId}' has generated new 2FA recovery codes.", userId);
        StatusMessage = "Созданы новые коды восстановления.";
        return RedirectToPage("./ShowRecoveryCodes");
    }
}
