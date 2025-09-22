using Auth.Api.Data;
using Auth.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationUserManager _userManager;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        ApplicationUserManager userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;

        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = null!;

    public string? ReturnUrl { get; set; }

    public IEnumerable<AuthenticationScheme> ExternalLogins { get; set; } = null!;

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        ExternalLogins = await _signInManager.GetExternalAuthenticationSchemesAsync();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = await _signInManager.GetExternalAuthenticationSchemesAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var createResult = await _userManager.RegisterUserAsync(Input.UserName, Input.Email, Input.Password);
        if (createResult.TempUser != null)
        {
            _logger.LogInformation("User created a new account with password.");

            return RedirectToPage("ConfirmEmailCode", new
            {
                userId = createResult.TempUser.Id,
                returnUrl,
            });
        }

        foreach (var error in createResult.Result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }

    public class InputModel
    {
        [Required]
        [Display(Name = "Логин")]
        public string UserName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [Display(Name = "Почта")]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(100, ErrorMessage = "Поле {0} должно быть длиной от {2} до {1} символов.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Display(Name = "Повтор пароля")]
        [Compare("Password", ErrorMessage = "Пароль и подтверждение не совпадают.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
