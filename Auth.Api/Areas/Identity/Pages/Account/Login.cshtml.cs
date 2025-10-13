using Auth.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Areas.Identity.Pages.Account;

public class LoginModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ILogger<LoginModel> logger) : PageModel
{
    [BindProperty]
    public required InputModel Input { get; set; }

    public IEnumerable<AuthenticationScheme> ExternalLogins { get; set; } = [];

    public string ReturnUrl { get; set; } = string.Empty;

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ExternalLogins = await signInManager.GetExternalAuthenticationSchemesAsync();
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = await signInManager.GetExternalAuthenticationSchemesAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var loginIdentifier = Input.UserNameOrEmail.Trim();
        if (string.IsNullOrEmpty(loginIdentifier))
        {
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
            return Page();
        }

        var user = await userManager.FindByNameAsync(loginIdentifier);
        if (user is null && loginIdentifier.Contains('@', StringComparison.Ordinal))
        {
            user = await userManager.FindByEmailAsync(loginIdentifier);
        }

        if (user is null || string.IsNullOrEmpty(user.UserName))
        {
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(user.UserName, Input.Password, Input.RememberMe, false);
        if (result.Succeeded)
        {
            logger.LogInformation("User logged in.");
            return LocalRedirect(returnUrl);
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("./LoginWith2fa", new
            {
                ReturnUrl = returnUrl,
                Input.RememberMe,
            });
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("User account locked out.");
            return RedirectToPage("./Lockout");
        }

        ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
        return Page();
    }

    public class InputModel
    {
        [Required]
        [Display(Name = "Логин или почта")]
        public required string UserNameOrEmail { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public required string Password { get; set; }

        [Display(Name = "Запомнить меня")]
        public required bool RememberMe { get; set; }
    }
}
