
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
    private readonly ApplicationDbContext _context;

    public ConfirmEmailCodeModel(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<ConfirmEmailCodeModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _context = context;
    }

    [BindProperty]
    public InputModel? Input { get; set; }

    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? ReturnUrl { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

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

        UserId = userId;
        Email = user.Email;
        ReturnUrl = returnUrl ?? Url.Content("~/");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string userId, string? returnUrl = null)
    {
        UserId = userId;
        ReturnUrl = returnUrl ?? Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Пользователь не найден.");
            return Page();
        }

        Email = user.Email;

        if (user.EmailConfirmCode != Input?.Code)
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

        var anotherUsers = _context.Users.Where(x => x.EmailConfirmed == false && (x.NormalizedEmail == user.NormalizedEmail || x.NormalizedUserName == user.NormalizedUserName));
        _context.Users.RemoveRange(anotherUsers);
        _context.SaveChanges();

        _logger.LogInformation("User {Email} confirmed their email with code", user.Email);
        // Автоматический вход после подтверждения почты
        await _signInManager.SignInAsync(user, false);
        StatusMessage = "Почта успешно подтверждена. Вы успешно вошли в систему.";

        return LocalRedirect(ReturnUrl);
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
