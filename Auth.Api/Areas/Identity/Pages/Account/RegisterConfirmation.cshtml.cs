#nullable disable

using Auth.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Auth.Api.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterConfirmationModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _sender;

    public RegisterConfirmationModel(UserManager<ApplicationUser> userManager, IEmailSender sender)
    {
        _userManager = userManager;
        _sender = sender;
    }

    /// <summary>
    /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public bool DisplayConfirmAccountLink { get; set; }

    /// <summary>
    /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string EmailConfirmationUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
    {
        if (email == null)
        {
            return RedirectToPage("/Index");
        }

        returnUrl = returnUrl ?? Url.Content("~/");

        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return NotFound($"Не удалось найти пользователя с почтой '{email}'.");
        }

        Email = email;
        DisplayConfirmAccountLink = false;

        return Page();
    }
}
