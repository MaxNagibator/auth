using Auth.Api.Data;
using Auth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ConfirmEmailCodeModel : PageModel
{
    private readonly ApplicationUserManager _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ConfirmEmailCodeModel(
        ApplicationDbContext context,
        ApplicationUserManager userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<ConfirmEmailCodeModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = null!;

    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? ReturnUrl { get; set; }

    //[TempData]
    public string? StatusMessage { get; set; }

    //[TempData]
    public string? ResendMessage { get; set; }
    //[TempData]
    public string? ResendFailMessage { get; set; }

    public async Task<IActionResult> OnGet(string userId, string? returnUrl = null)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Index");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return RedirectToPage("/Index");
        }

        UserId = user.Id;
        Email = user.Email;
        ReturnUrl = returnUrl ?? Url.Content("~/");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string userId, string? returnUrl = null)
    {
        UserId = userId;
        ReturnUrl = returnUrl ?? Url.Content("~/");

        if (!ModelState.IsValid || string.IsNullOrEmpty(Input.Code))
        {
            return Page();
        }

        var confirmedUser = await _userManager.ConfirmEmail(userId, Input.Code);
        if (confirmedUser == null)
        {
            ModelState.AddModelError(string.Empty, "Неверный код подтверждения.");
            return Page();
        }

        StatusMessage = "Почта успешно подтверждена. Вы успешно вошли в систему.";

        await _signInManager.SignInAsync(confirmedUser, false);
        return LocalRedirect(ReturnUrl);
    }

    public async Task<IActionResult> OnPostResendEmailConfirmationAsync(string userId, string? returnUrl = null)
    {
        ResendMessage = null;
        ResendFailMessage = null;

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return RedirectToPage("/Index");
        }

        UserId = userId;
        ReturnUrl = returnUrl ?? Url.Content("~/");

        var message = await _userManager.ResendVerificationMail(user.Id);
        if (message == null)
        {
            ResendMessage = "Новый код отправлен. Проверьте почту.";
        }
        else
        {
            ResendFailMessage = message;
        }
        return Page();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Введите код подтверждения")]
        [Display(Name = "Код подтверждения")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "Код должен содержать 8 цифр")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "Код должен содержать только цифры")]
        public string? Code { get; set; }
    }
}
