using Auth.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Services;

public class ApplicationUserManager
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmailSender _emailSender;

    public ApplicationUserManager(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _emailSender = emailSender;
    }

    public async Task<TempApplicationUser?> FindByIdAsync(string userId)
    {
        return await _dbContext.TempApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<(IdentityResult Result, TempApplicationUser? TempUser)> RegisterUserAsync(
        string userName, string email, string password)
    {
        var tempEmail = $"temp_{Guid.NewGuid()}@example.com";
        var tempUsername = $"temp_{Guid.NewGuid()}";

        // Создаём объект ApplicationUser с оригинальными данными пользователя,
        // но ещё не создаём его в базе — нужно сначала проверить валидность
        // введённых данных через стандартные валидаторы Identity.
        var user = new ApplicationUser
        {
            Email = email,
            UserName = userName,
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.MaxValue
        };

        // Валидация username и email оригинальных данных
        foreach (var validator in _userManager.UserValidators)
        {
            var result = await validator.ValidateAsync(_userManager, user);
            if (!result.Succeeded)
                return (IdentityResult.Failed(result.Errors.ToArray()), null);
        }

        // Валидация пароля оригинальных данных
        foreach (var validator in _userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(_userManager, user, password);
            if (!result.Succeeded)
                return (IdentityResult.Failed(result.Errors.ToArray()), null);
        }

        // После успешной валидации меняем Email/Username на временные,
        // Чтобы юзер не смог войти до подтверждения.
        user.Email = tempEmail;
        user.UserName = tempUsername;

        // Здесь произойдет повторная валидация под капотом, но это не страшно потому что данные временные
        // Мы могли бы изначально создать ApplicationUser с вводом от пользователя и делегировать валидацию на _userManager.CreateAsync
        // Но тогда на бы пришлось обновлять его данные на временные уже после создания и был бы микро шанс на коллизию 
        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
            return (createResult, null);

        // Создаём запись в TempApplicationUser с реальными данными и кодом подтверждения
        var tempUser = new TempApplicationUser
        {
            Email = email,
            UserName = userName,
            EmailConfirmCode = GetCode(8),
            ApplicationUserId = user.Id,
        };

        _dbContext.TempApplicationUsers.Add(tempUser);

        await _dbContext.SaveChangesAsync();
        await SendVerificationMail(userName, email, tempUser.EmailConfirmCode);

        return (IdentityResult.Success, tempUser);
    }

    public async Task<ApplicationUser?> ConfirmEmail(string userId, string code)
    {
        var tempUser = await _dbContext.TempApplicationUsers.FindAsync(userId);
        if (tempUser == null || tempUser.EmailConfirmCode != code)
            return null;

        var applicationUser = await _userManager.FindByIdAsync(tempUser.ApplicationUserId);
        if (applicationUser == null)
            return null;

        applicationUser.Email = tempUser.Email;
        applicationUser.UserName = tempUser.UserName;

        applicationUser.LockoutEnd = null;
        applicationUser.LockoutEnabled = false;
        applicationUser.EmailConfirmed = true;

        await _userManager.UpdateAsync(applicationUser);
        await _dbContext.TempApplicationUsers.Where(u => u.Email == tempUser.Email).ExecuteDeleteAsync();

        return applicationUser;
    }

    public async Task<bool> ResendVerificationMail(string email)
    {
        var tempUser = await _dbContext.TempApplicationUsers.FirstOrDefaultAsync(u => u.Email == email);
        if (tempUser == null)
            return false;

        tempUser.EmailConfirmCode = GetCode(8);
        await _dbContext.SaveChangesAsync();

        await SendVerificationMail(tempUser.UserName, tempUser.Email, tempUser.EmailConfirmCode);
        return true;
    }

    private Task SendVerificationMail(string userName, string email, string confirmCode)
    {
        const string title = "Подтверждение регистрации";
        var body =
            $"Здравствуйте, {userName}!\r\nВаш код для подтверждения регистрации на сайте bob217.auth:\r\n{confirmCode}";

        return _emailSender.SendEmailAsync(email, title, body);
    }

    private static string GetCode(int length)
    {
        const string chars = "1234567890";

        var bytes = new byte[length];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();

        rng.GetBytes(bytes);
        return new(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }
}
