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

    public string UserId { get; set; }
    public string Email { get; set; }

    public async Task<IActionResult> OnGetAsync(string userId, string email, string returnUrl = null)
    {
        if (userId == null)
        {
            return RedirectToPage("/Index");
        }

        returnUrl = returnUrl ?? Url.Content("~/");

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound($"Не удалось найти пользователя");
        }

        UserId = userId;
        Email = email;

        return Page();
    }
}
