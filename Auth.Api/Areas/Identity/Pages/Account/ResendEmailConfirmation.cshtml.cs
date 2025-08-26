#nullable disable

using Auth.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Auth.Api.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ResendEmailConfirmationModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;

    public ResendEmailConfirmationModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
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

        var user = await _userManager.FindByEmailAsync(Input.Email);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Письмо отправлено. Проверьте почту.");
            return Page();
        }

        // Check if email is already confirmed
        if (user.EmailConfirmed)
        {
            ModelState.AddModelError(string.Empty, "Ваша почта уже подтверждена.");
            return Page();
        }

        // Generate new confirmation code
        var confirmCode = GetCode(8);
        user.EmailConfirmCode = confirmCode;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Ошибка при генерации нового кода.");
            return Page();
        }

        // Send email with the new code
        const string title = "Повторное подтверждение регистрации";
        var body = $"Здравствуйте, {user.UserName}!\r\nВаш новый код для подтверждения регистрации на сайте bob217.auth:\r\n{confirmCode}";

        await _emailSender.SendEmailAsync(Input.Email, title, body);

        ModelState.AddModelError(string.Empty, "Новый код отправлен. Проверьте почту.");
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
