#nullable disable

using Auth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ResendEmailConfirmationModel : PageModel
{
    private readonly ApplicationUserManager _userManager;

    public ResendEmailConfirmationModel(ApplicationUserManager userManager)
    {
        _userManager = userManager;
    }

    /// <summary>
    /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; }

    public void OnGet(string email = null)
    {
        if (!string.IsNullOrEmpty(email))
        {
            Input = new()
                { Email = email };
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var emailSend = await _userManager.ResendVerificationMail(Input.Email);

        if (!emailSend)
        {
            ModelState.AddModelError(string.Empty, "Не удалось отправить новый код.");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "Новый код отправлен. Проверьте почту.");
        return Page();
    }

    /// <summary>
    /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        /// directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Required]
        [EmailAddress]
        [Display(Name = "Почта")]
        public string Email { get; set; }
    }
}
