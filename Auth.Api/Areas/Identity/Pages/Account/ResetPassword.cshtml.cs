using Auth.Api.Data;
using Auth.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Auth.Api.Areas.Identity.Pages.Account;

public class ResetPasswordModel(
    UserManager<ApplicationUser> userManager,
    ApplicationUserManager applicationUserManager)
    : PageModel
{
    [BindProperty]
    public required InputModel Input { get; set; }

    public IActionResult OnGet(string? code = null, string? email = null)
    {
        if (code == null || email == null)
        {
            return BadRequest("Для сброса пароля нужен код и почта.");
        }

        Input = new()
        {
            Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)),
            Email = email,
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        var result = await userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
        if (result.Succeeded)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await applicationUserManager.SendPasswordChangedNotificationAsync(Input.Email, ipAddress);
        }

        return RedirectToPage("./ResetPasswordConfirmation");
    }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Почта")]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(100, ErrorMessage = "{0} должен быть от {2} до {1} символов.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Display(Name = "Повторите пароль")]
        [Compare("Password", ErrorMessage = "Пароль и подтверждение не совпадают.")]
        public string? ConfirmPassword { get; set; }

        [Required]
        public string Code { get; set; } = null!;
    }
}
