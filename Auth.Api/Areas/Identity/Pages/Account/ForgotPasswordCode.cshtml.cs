using Auth.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace Auth.Api.Areas.Identity.Pages.Account;

public class ForgotPasswordCodeModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender) : PageModel
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

        var user = await userManager.FindByEmailAsync(email);
        if (user != null && !await userManager.IsEmailConfirmedAsync(user))
        {
            return RedirectToPage("./ForgotPassword");
        }

        Input = new()
        {
            Email = email,
        };

        await LoadCooldownAsync(user);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            var existingUser = await userManager.FindByEmailAsync(Input.Email);
            await LoadCooldownAsync(existingUser);
            return Page();
        }

        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Неверный код подтверждения.");
            await LoadCooldownAsync(null);
            return Page();
        }

        if (!await userManager.IsEmailConfirmedAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Неверный код подтверждения.");
            await LoadCooldownAsync(user);
            return Page();
        }

        var storedCode = await userManager.GetAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.TokenName);

        var timestampToken = await userManager.GetAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.TimestampTokenName);

        if (string.IsNullOrWhiteSpace(storedCode) || string.IsNullOrWhiteSpace(timestampToken) || !PasswordResetDefaults.TryParseTimestamp(timestampToken, out var sentAtUtc))
        {
            await ClearPasswordResetTokensAsync(user);
            ResendMessage = "Запросите новый код. Текущий код недоступен.";
            ModelState.AddModelError(string.Empty, ResendMessage);
            await LoadCooldownAsync(user);
            return Page();
        }

        if (PasswordResetDefaults.IsCodeExpired(sentAtUtc))
        {
            await ClearPasswordResetTokensAsync(user);
            ResendMessage = "Срок действия кода истёк. Запросите новый код.";
            ModelState.AddModelError(string.Empty, ResendMessage);
            await LoadCooldownAsync(user);
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

            await LoadCooldownAsync(user);
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

    public async Task<IActionResult> OnPostResendAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.Email))
        {
            ModelState.AddModelError(string.Empty, "Заполните данные формы.");
            return Page();
        }

        var email = Input.Email;
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

            await LoadCooldownAsync(null);
            return Page();
        }

        if (!await userManager.IsEmailConfirmedAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Неверный код подтверждения.");
            await LoadCooldownAsync(user);
            return Page();
        }

        var timestampToken = await userManager.GetAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.TimestampTokenName);

        if (!string.IsNullOrEmpty(timestampToken) && PasswordResetDefaults.TryParseTimestamp(timestampToken, out var lastSentUtc))
        {
            var remaining = PasswordResetDefaults.GetRemainingCooldown(lastSentUtc);
            if (remaining > TimeSpan.Zero)
            {
                ModelState.AddModelError(string.Empty,
                    $"Повторно отправить код можно через {Math.Ceiling(remaining.TotalSeconds)} секунд.");

                await LoadCooldownAsync(user);
                return Page();
            }
        }

        var verificationCode = PasswordResetDefaults.GenerateCode();
        await userManager.SetAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.TokenName,
            verificationCode);

        await userManager.SetAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.TimestampTokenName,
            PasswordResetDefaults.GetCurrentTimestampString());

        await userManager.SetAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.AttemptTokenName,
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

        await LoadCooldownAsync(user);

        return Page();
    }

    private async Task LoadCooldownAsync(ApplicationUser? user)
    {
        if (user == null)
        {
            // TODO: Уязвимость
            CooldownRemainingSeconds = Math.Ceiling(PasswordResetDefaults.ResendCooldown.TotalSeconds);
            return;
        }

        var timestampToken = await userManager.GetAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.TimestampTokenName);

        if (!string.IsNullOrEmpty(timestampToken) && PasswordResetDefaults.TryParseTimestamp(timestampToken, out var lastSentUtc))
        {
            CooldownRemainingSeconds = Math.Ceiling(PasswordResetDefaults.GetRemainingCooldown(lastSentUtc).TotalSeconds);
        }
    }

    private async Task<int> IncrementAttemptAsync(ApplicationUser user)
    {
        var attemptsToken = await userManager.GetAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.AttemptTokenName);

        var attempts = 0;
        if (!string.IsNullOrEmpty(attemptsToken)
            && int.TryParse(attemptsToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            attempts = parsed;
        }

        attempts++;

        await userManager.SetAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.AttemptTokenName,
            attempts.ToString(CultureInfo.InvariantCulture));

        return attempts;
    }

    private async Task ClearPasswordResetTokensAsync(ApplicationUser user)
    {
        await userManager.RemoveAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.TokenName);

        await userManager.RemoveAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.TimestampTokenName);

        await userManager.RemoveAuthenticationTokenAsync(user,
            PasswordResetDefaults.LoginProvider,
            PasswordResetDefaults.AttemptTokenName);
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
