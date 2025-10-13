using Auth.Api.Data;
using Auth.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace Auth.Api.Areas.Identity.Pages.Account;

public class ForgotPasswordCodeModel(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    ApplicationUserManager applicationUserManager) : PageModel
{
    [BindProperty]
    public required InputModel Input { get; set; }

    public double CooldownRemainingSeconds { get; private set; }
    public bool CanResend => CooldownRemainingSeconds <= 0;
    public string? ResendMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToPage("./ForgotPassword");
        }

        Input = new()
        {
            Email = email,
        };

        GetLastSentUtc(email);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var lastSentUtc = GetLastSentUtc(Input.Email);

        if (!ModelState.IsValid)
        {
            var existingUser = await userManager.FindByEmailAsync(Input.Email);
            return Page();
        }

        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            // todo по коду ошибки по прежнему можно понять есть пользователь с таким ЕМАИЛ или нет на сайте. нужно попытки перенести в таблицу отправки кода
            ModelState.AddModelError(string.Empty, "Неверный код подтверждения.");
            return Page();
        }

        if (!await userManager.IsEmailConfirmedAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Неверный код подтверждения.");
            return Page();
        }

        var storedCode = await userManager.GetAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.EmailVerificationCodeTokenName);

        if (lastSentUtc == null)
        {
            await ClearPasswordResetTokensAsync(user);
            ResendMessage = "Запросите новый код. Текущий код недоступен.";
            ModelState.AddModelError(string.Empty, ResendMessage);
            return Page();
        }

        if (PasswordResetDefaults.IsCodeExpired(lastSentUtc.Value))
        {
            await ClearPasswordResetTokensAsync(user);
            ResendMessage = "Срок действия кода истёк. Запросите новый код.";
            ModelState.AddModelError(string.Empty, ResendMessage);
            return Page();
        }

        if (storedCode != Input.Code)
        {
            var attempts = await IncrementAttemptAsync(user);
            var remainingAttempts = PasswordResetDefaults.MaxAttempts - attempts;

            if (remainingAttempts <= 0)
            {
                await ClearPasswordResetTokensAsync(user);
                ResendMessage = "Количество попыток исчерпано. Запросите новый код.";
                ModelState.AddModelError(string.Empty, ResendMessage);
            }
            else
            {
                ModelState.AddModelError(string.Empty, $"Неверный код подтверждения. Осталось попыток: {remainingAttempts}.");
            }

            return Page();
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

        await ClearPasswordResetTokensAsync(user);

        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));

        return RedirectToPage("./ResetPassword", new
        {
            area = "Identity",
            code = encodedToken,
            email = Input.Email,
        });
    }

    private DateTime? GetLastSentUtc(string email)
    {
        var lastSentUtc = applicationUserManager.GetRestorePasswordDate(email);
        if (lastSentUtc != null)
        {
            CooldownRemainingSeconds = Math.Ceiling(PasswordResetDefaults.GetRemainingCooldown(lastSentUtc.Value).TotalSeconds);
        }

        return lastSentUtc;
    }

    public async Task<IActionResult> OnPostResendAsync()
    {
        var email = Input.Email;

        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(string.Empty, "Заполните данные формы.");
            return Page();
        }

        var lastSentUtc = GetLastSentUtc(email);
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            ResendMessage = "Новый код отправлен. Проверьте почту.";
            ModelState.Clear();
            Input = new()
            {
                Email = email,
                Code = string.Empty,
            };

            return Page();
        }

        if (!await userManager.IsEmailConfirmedAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Неверный код подтверждения.");
            return Page();
        }

        if (lastSentUtc != null)
        {
            var remaining = PasswordResetDefaults.GetRemainingCooldown(lastSentUtc.Value);
            if (remaining > TimeSpan.Zero)
            {
                ModelState.AddModelError(string.Empty,
                    $"Повторно отправить код можно через {Math.Ceiling(remaining.TotalSeconds)} секунд.");

                return Page();
            }
        }

        var verificationCode = PasswordResetDefaults.GenerateCode();
        await userManager.SetAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.EmailVerificationCodeTokenName,
            verificationCode);


        applicationUserManager.SetRestorePasswordDate(email, DateTime.UtcNow);

        await userManager.SetAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.EmailVerificationCodeAttemptsTokenName,
            "0");

        await emailSender.SendEmailAsync(email,
            PasswordResetDefaults.EmailSubject,
            $"Ваш код для сброса пароля: {verificationCode}");

        ResendMessage = "Новый код отправлен. Проверьте почту.";
        ModelState.Clear();
        Input = new()
        {
            Email = email,
            Code = string.Empty,
        };

        return Page();
    }

    //private async Task LoadCooldownAsync(string email)
    //{
    //    var lastSentUtc = applicationUserManager.GetRestorePasswordDate(email);
    //    if (lastSentUtc != null)
    //    {
    //        CooldownRemainingSeconds = Math.Ceiling(PasswordResetDefaults.GetRemainingCooldown(lastSentUtc.Value).TotalSeconds);
    //    }
    //}

    private async Task<int> IncrementAttemptAsync(ApplicationUser user)
    {
        var attemptsToken = await userManager.GetAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.EmailVerificationCodeAttemptsTokenName);

        var attempts = 0;
        if (!string.IsNullOrEmpty(attemptsToken)
            && int.TryParse(attemptsToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            attempts = parsed;
        }

        attempts++;

        await userManager.SetAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.EmailVerificationCodeAttemptsTokenName,
            attempts.ToString(CultureInfo.InvariantCulture));

        return attempts;
    }

    private async Task ClearPasswordResetTokensAsync(ApplicationUser user)
    {
        await userManager.RemoveAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.EmailVerificationCodeTokenName);

        applicationUserManager.SetRestorePasswordDate(user.Email!, null);

        await userManager.RemoveAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.EmailVerificationCodeAttemptsTokenName);
    }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Почта")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Введите код, отправленный на почту")]
        [Display(Name = "Код подтверждения")]
        [StringLength(PasswordResetDefaults.CodeLength, MinimumLength = PasswordResetDefaults.CodeLength, ErrorMessage = "Код должен содержать {1} цифр")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Код должен содержать только цифры")]
        public string? Code { get; set; }
    }
}
