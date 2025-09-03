#nullable disable

using Auth.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace Auth.Api.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ExternalLoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ExternalLoginModel> _logger;

    public ExternalLoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<ExternalLoginModel> logger,
        IEmailSender emailSender)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _emailSender = emailSender;
    }


    [BindProperty]
    public InputModel Input { get; set; }

    public string ProviderDisplayName { get; set; }

    public string ReturnUrl { get; set; }

    [TempData]
    public string ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        return RedirectToPage("./Login");
    }

    public IActionResult OnPost(string provider, string returnUrl = null)
    {
        var redirectUrl = Url.Page("./ExternalLogin", "Callback", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (remoteError != null)
        {
            ErrorMessage = $"Ошибка внешнего провайдера: {remoteError}";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();

        if (info == null)
        {
            ErrorMessage = "Не удалось загрузить данные внешнего входа.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }


        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false, true);
        if (result.Succeeded)
        {
            _logger.LogInformation($"{info.Principal.Identity.Name} logged in with {info.LoginProvider} provider.");
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            return RedirectToPage("./Lockout");
        }

        ReturnUrl = returnUrl;
        ProviderDisplayName = info.ProviderDisplayName;

        if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
        {
            Input = new()
            {
                Email = info.Principal.FindFirstValue(ClaimTypes.Email),
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ErrorMessage = "Не удалось загрузить данные внешнего входа при подтверждении.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        ReturnUrl = returnUrl;
        ProviderDisplayName = info.ProviderDisplayName;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new ApplicationUser();
        await _userManager.SetUserNameAsync(user, Input.Email);
        await _userManager.SetEmailAsync(user, Input.Email);

        var result = await _userManager.CreateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        result = await _userManager.AddLoginAsync(user, info);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }


        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

        var userId = await _userManager.GetUserIdAsync(user);
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var callbackUrl = Url.Page("/Account/ConfirmEmail",
            null,
            new
            {
                area = "Identity",
                userId,
                code,
            },
            Request.Scheme
        );

        await _emailSender.SendEmailAsync(Input.Email,
            "Подтверждение почты",
            $"Подтвердите аккаунт: <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>перейдите по ссылке</a>."
        );

        if (_userManager.Options.SignIn.RequireConfirmedAccount)
        {
            return RedirectToPage("./RegisterConfirmation", new
            {
                Input.Email,
            });
        }

        await _signInManager.SignInAsync(user, false, info.LoginProvider);
        return LocalRedirect(returnUrl);
    }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Почта")]
        public string Email { get; set; }
    }
}
