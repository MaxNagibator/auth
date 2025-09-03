using Auth.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Auth.Api.Areas.Identity.Pages.Account;

// todo вынести

public class RegisterModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserStore<ApplicationUser> _userStore;
    private readonly IUserEmailStore<ApplicationUser> _emailStore;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IEmailSender _emailSender;
    private readonly ApplicationDbContext _applicationDbContext;

    public RegisterModel(
        ApplicationDbContext applicationDbContext,
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore();
        _signInManager = signInManager;
        _logger = logger;
        _emailSender = emailSender;
        _applicationDbContext = applicationDbContext;
    }

    [BindProperty]
    public InputModel? Input { get; set; }

    public string? ReturnUrl { get; set; }

    public IList<AuthenticationScheme>? ExternalLogins { get; set; }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (ModelState.IsValid)
        {
            var email = Input?.Email?.ToUpper();
            var emailBusy = _applicationDbContext.Users.Any(x => x.NormalizedEmail == email && x.EmailConfirmed == true);
            if (emailBusy)
            {
                ModelState.AddModelError(nameof(Input.Email), "Почта занята");
                return Page();
            }

            var user = CreateUser();
            await _userStore.SetUserNameAsync(user, Input?.UserName, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input?.Email, CancellationToken.None);
            var confirmCode = GetCode(8);
            user.EmailConfirmCode = confirmCode;
            var result = await _userManager.CreateAsync(user, Input?.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                var userId = await _userManager.GetUserIdAsync(user);

                await SendEmail(Input.UserName, Input!.Email, confirmCode);

                return RedirectToPage("RegisterConfirmation", new
                {
                    userId,
                    email = Input!.Email,
                    returnUrl,
                });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
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
        const string Title = "Подтверждение регистрации";
        var body = $"Здравствуйте, {userName}!\r\nВаш код для подтверждения регистрации на сайте bob217.auth:\r\n{confirmCode}";
        return _emailSender.SendEmailAsync(email, Title, body);
    }

    private ApplicationUser CreateUser()
    {
        try
        {
            return Activator.CreateInstance<ApplicationUser>();
        }
        catch
        {
            throw new InvalidOperationException($"Не удалось создать экземпляр '{nameof(ApplicationUser)}'. " + $"Убедитесь, что '{nameof(ApplicationUser)}' не абстрактный класс и имеет конструктор без параметров, либо переопределите страницу регистрации.");
        }
    }

    private IUserEmailStore<ApplicationUser> GetEmailStore()
    {
        if (!_userManager.SupportsUserEmail)
        {
            throw new NotSupportedException("The default UI requires a user store with email support.");
        }

        return (IUserEmailStore<ApplicationUser>)_userStore;
    }

    public class InputModel
    {
        [Required]
        [Display(Name = "Логин")]
        public string? UserName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Почта")]
        public string? Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Поле {0} должно быть длиной от {2} до {1} символов.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Повтор пароля")]
        [Compare("Password", ErrorMessage = "Пароль и подтверждение не совпадают.")]
        public string? ConfirmPassword { get; set; }
    }
}
