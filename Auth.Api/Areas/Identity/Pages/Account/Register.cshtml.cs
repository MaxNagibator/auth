using Auth.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Auth.Api.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IEmailSender _emailSender;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _emailSender = emailSender;
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

        var existingUser = await _userManager.FindByEmailAsync(Input.Email);
        if (existingUser is { EmailConfirmed: true })
        {
            ModelState.AddModelError(string.Empty, "Почта занята");
            return Page();
        }

        var confirmCode = GetCode(8);
        var user = new ApplicationUser
        {
            EmailConfirmCode = confirmCode
        };

        await _userManager.SetEmailAsync(user, Input.Email);
        await _userManager.SetUserNameAsync(user, Input.UserName);
        var result = await _userManager.CreateAsync(user, Input.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User created a new account with password.");
            
            await SendEmail(Input.UserName, Input.Email, confirmCode);
            return RedirectToPage("RegisterConfirmation", new
            {
                userId = user.Id,
                returnUrl,
            });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }

    private static string GetCode(int length, string allowedChars = "1234567890")
    {
        var result = new StringBuilder(length);

        while (result.Length < length)
        {
            var index = Random.Shared.Next(allowedChars.Length);
            result.Append(allowedChars[index]);
        }

        return result.ToString();
    }

    private Task SendEmail(string userName, string email, string confirmCode)
    {
        const string title = "Подтверждение регистрации";
        var body =
            $"Здравствуйте, {userName}!\r\nВаш код для подтверждения регистрации на сайте bob217.auth:\r\n{confirmCode}";
        return _emailSender.SendEmailAsync(email, title, body);
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
