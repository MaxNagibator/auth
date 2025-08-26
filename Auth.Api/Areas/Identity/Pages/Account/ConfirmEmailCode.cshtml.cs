#nullable disable

using Auth.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ConfirmEmailCodeModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<ConfirmEmailCodeModel> _logger;

    public ConfirmEmailCodeModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<ConfirmEmailCodeModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public string Email { get; set; }
    public string ReturnUrl { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    public IActionResult OnGet(string email, string returnUrl = null)
    {
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToPage("/Index");
        }

        Email = email;
        ReturnUrl = returnUrl ?? Url.Content("~/");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string email, string returnUrl = null)
    {
        Email = email;
        ReturnUrl = returnUrl ?? Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Пользователь не найден.");
            return Page();
        }

        if (user.EmailConfirmCode != Input.Code)
        {
            ModelState.AddModelError(string.Empty, "Неверный код подтверждения.");
            return Page();
        }

        user.EmailConfirmed = true;
        user.EmailConfirmCode = null;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        _logger.LogInformation("User {Email} confirmed their email with code", email);
        StatusMessage = "Почта успешно подтверждена! Теперь вы можете войти в систему.";

        return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Введите код подтверждения")]
        [Display(Name = "Код подтверждения")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "Код должен содержать 8 цифр")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "Код должен содержать только цифры")]
        public string Code { get; set; }
    }
}
