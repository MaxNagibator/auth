using Auth.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;

namespace Auth.Api.Areas.Identity.Pages.Account;

internal static class PasswordResetDefaults
{
    public const string LoginProvider = "PasswordReset";
    public const string TokenName = "EmailVerificationCode";
    public const int CodeLength = 8;
    public const string TimestampTokenName = "EmailVerificationCodeTimestamp";
    public const string AttemptTokenName = "EmailVerificationCodeAttempts";
    public const string EmailSubject = "Сброс пароля";
    public const int MaxAttempts = 5;

    public static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

    public static string GenerateCode()
    {
        const string Digits = "0123456789";

        var buffer = new byte[CodeLength];
        RandomNumberGenerator.Fill(buffer);

        var chars = new char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
        {
            chars[i] = Digits[buffer[i] % Digits.Length];
        }

        return new(chars);
    }

    public static string GetCurrentTimestampString()
    {
        return DateTime.UtcNow.ToString("O");
    }

    public static bool TryParseTimestamp(string value, out DateTime timestampUtc)
    {
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out timestampUtc);
    }

    public static TimeSpan GetRemainingCooldown(DateTime sentAtUtc)
    {
        var remaining = ResendCooldown - (DateTime.UtcNow - sentAtUtc);
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public static bool IsCodeExpired(DateTime sentAtUtc)
    {
        return DateTime.UtcNow - sentAtUtc > CodeLifetime;
    }
}

public class ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender) : PageModel
{
    [BindProperty]
    public required InputModel Input { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await userManager.FindByEmailAsync(Input.Email);

        if (user == null)
        {
            return RedirectToPage("./ForgotPasswordCode", new { email = Input.Email });
        }

        // TODO: Возможно избыточно
        if (!await userManager.IsEmailConfirmedAsync(user))
        {
            return RedirectToPage("./ForgotPasswordConfirmation");
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

        await emailSender.SendEmailAsync(Input.Email,
            PasswordResetDefaults.EmailSubject,
            $"Ваш код для сброса пароля: {verificationCode}");

        return RedirectToPage("./ForgotPasswordCode", new { email = Input.Email });
    }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Почта")]
        public required string Email { get; set; }
    }
}
