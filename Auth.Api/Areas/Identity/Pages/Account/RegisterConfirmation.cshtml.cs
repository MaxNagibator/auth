using Auth.Api.Data;
using Auth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Auth.Api.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterConfirmationModel : PageModel
{
    private readonly ApplicationUserManager _userManager;

    public RegisterConfirmationModel(ApplicationUserManager userManager)
    {
        _userManager = userManager;
    }

    public string UserId { get; set; } = null!;
    public string Email { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Index");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("Не удалось найти пользователя");
        }

        UserId = userId;
        Email = user.Email;
        
        return Page();
    }
}
