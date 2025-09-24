using Auth.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Auth.Api.Services;

public sealed class ApplicationUserManager(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    IEmailSender emailSender,
    IOptions<ServiceInfoSettings> serviceInfoOptions)
{
    private readonly ServiceInfoSettings _serviceInfoSettings = serviceInfoOptions.Value;

    public Task<TempApplicationUser?> FindByIdAsync(string userId)
    {
        return dbContext.TempApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<(IdentityResult Result, TempApplicationUser? TempUser)> RegisterUserAsync(
        string userName,
        string email,
        string password)
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
            LockoutEnd = DateTimeOffset.MaxValue,
        };

        // Валидация username и email оригинальных данных
        foreach (var validator in userManager.UserValidators)
        {
            var result = await validator.ValidateAsync(userManager, user);
            if (!result.Succeeded)
            {
                return (IdentityResult.Failed(result.Errors.ToArray()), null);
            }
        }

        // Валидация пароля оригинальных данных
        foreach (var validator in userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(userManager, user, password);
            if (!result.Succeeded)
            {
                return (IdentityResult.Failed(result.Errors.ToArray()), null);
            }
        }

        // После успешной валидации меняем Email/Username на временные,
        // Чтобы юзер не смог войти до подтверждения.
        user.Email = tempEmail;
        user.UserName = tempUsername;

        // Здесь произойдет повторная валидация под капотом, но это не страшно потому что данные временные
        // Мы могли бы изначально создать ApplicationUser с вводом от пользователя и делегировать валидацию на _userManager.CreateAsync
        // Но тогда на бы пришлось обновлять его данные на временные уже после создания и был бы микро шанс на коллизию 
        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return (createResult, null);
        }

        // Создаём запись в TempApplicationUser с реальными данными и кодом подтверждения
        var tempUser = new TempApplicationUser
        {
            Email = email,
            UserName = userName,
            ApplicationUserId = user.Id,
        };

        SetCode(tempUser);

        dbContext.TempApplicationUsers.Add(tempUser);

        await dbContext.SaveChangesAsync();
        await SendVerificationMail(userName, email, tempUser.EmailConfirmCode!);

        return (IdentityResult.Success, tempUser);
    }

    public async Task<ConfirmEmailResult> ConfirmEmail(string userId, string code)
    {
        // todo добавить интервал между попытками !!!
        var tempUser = await dbContext.TempApplicationUsers.FindAsync(userId);
        if (tempUser == null)
        {
            return new() { IsSuccess = false, Message = "Неверный код подтверждения." };
        }

        if (tempUser.IsEmailConfirmCodeBlock)
        {
            return new() { IsSuccess = false, Message = "Попытки кончились. Запросите код повторно." };
        }

        if (tempUser.EmailConfirmCode != code)
        {
            tempUser.EmailConfirmCodeAttemt++;

            if (tempUser.EmailConfirmCodeAttemt > 3)
            {
                tempUser.IsEmailConfirmCodeBlock = true;
            }

            await dbContext.SaveChangesAsync();

            return new() { IsSuccess = false, Message = "Неверный код подтверждения." };
        }

        var applicationUser = await userManager.FindByIdAsync(tempUser.ApplicationUserId);
        if (applicationUser == null)
        {
            throw new("applicationUser is null");
        }

        applicationUser.Email = tempUser.Email;
        applicationUser.UserName = tempUser.UserName;

        applicationUser.LockoutEnd = null;
        applicationUser.LockoutEnabled = false;
        applicationUser.EmailConfirmed = true;

        await userManager.UpdateAsync(applicationUser);
        // todo покрасивее ту ловеры можно обыграть
        await dbContext.TempApplicationUsers
            .Where(u => u.Email.ToLower() == tempUser.Email.ToLower() || u.UserName.ToLower() == tempUser.UserName.ToLower())
            .ExecuteDeleteAsync();

        return new() { IsSuccess = true, User = applicationUser };
    }

    public async Task<string?> ResendVerificationMail(string userId)
    {
        var tempUser = await dbContext.TempApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (tempUser == null)
        {
            return null;
        }

        if (tempUser.EmailConfirmCodeDate != null)
        {
            var sec = 60 - (int)(DateTime.UtcNow - tempUser.EmailConfirmCodeDate.Value).TotalSeconds;
            if (sec > 0)
            {
                return $"Повторно отправить код можно через {sec} секунд.";
            }
        }

        SetCode(tempUser);
        await dbContext.SaveChangesAsync();

        await SendVerificationMail(tempUser.UserName, tempUser.Email, tempUser.EmailConfirmCode!);
        return null;
    }

    private static void SetCode(TempApplicationUser tempUser)
    {
        tempUser.EmailConfirmCodeDate = DateTime.UtcNow;
        tempUser.EmailConfirmCode = GetCode(8);
        tempUser.EmailConfirmCodeAttemt = 0;
        tempUser.IsEmailConfirmCodeBlock = false;
    }

    private static string GetCode(int length)
    {
        const string Chars = "1234567890";

        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();

        rng.GetBytes(bytes);
        return new(bytes.Select(b => Chars[b % Chars.Length]).ToArray());
    }

    private Task SendVerificationMail(string userName, string email, string confirmCode)
    {
        var title = $"Подтверждение регистрации {_serviceInfoSettings.DisplayName}";
        var body = $"""
                    Здравствуйте, {userName}!
                    Ваш код для подтверждения регистрации на сайте:
                    {confirmCode}
                    """;

        return emailSender.SendEmailAsync(email, title, body);
    }

    public sealed class ConfirmEmailResult
    {
        public ApplicationUser? User { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
    }
}
