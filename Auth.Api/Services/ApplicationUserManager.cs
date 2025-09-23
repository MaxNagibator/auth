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
            ApplicationUserId = user.Id,
        };
        SetCode(tempUser);

        _dbContext.TempApplicationUsers.Add(tempUser);

        await _dbContext.SaveChangesAsync();
        await SendVerificationMail(userName, email, tempUser.EmailConfirmCode);

        return (IdentityResult.Success, tempUser);
    }

    public class ConfirmEmailResult
    {
        public ApplicationUser? User { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    public async Task<ConfirmEmailResult> ConfirmEmail(string userId, string code)
    {
        // todo добавить интервал между попытками !!!
        var tempUser = await _dbContext.TempApplicationUsers.FindAsync(userId);
        if (tempUser == null)
        {
            return new ConfirmEmailResult { IsSuccess = false, Message = "Неверный код подтверждения." };
        }

        if (tempUser.IsEmailConfirmCodeBlock)
        {
            return new ConfirmEmailResult { IsSuccess = false, Message = "Попытки кончились. Запросите код повторно." };
        }

        if (tempUser.EmailConfirmCode != code)
        {
            tempUser.EmailConfirmCodeAttemt++;

            if (tempUser.EmailConfirmCodeAttemt > 3)
            {
                tempUser.IsEmailConfirmCodeBlock = true;
            }

            await _dbContext.SaveChangesAsync();

            return new ConfirmEmailResult { IsSuccess = false, Message = "Неверный код подтверждения." };
        }

        var applicationUser = await _userManager.FindByIdAsync(tempUser.ApplicationUserId);
        if (applicationUser == null)
        {
            throw new Exception("applicationUser is null");
        }

        applicationUser.Email = tempUser.Email;
        applicationUser.UserName = tempUser.UserName;

        applicationUser.LockoutEnd = null;
        applicationUser.LockoutEnabled = false;
        applicationUser.EmailConfirmed = true;

        await _userManager.UpdateAsync(applicationUser);
        // todo покрасивее ту ловеры можно обыграть
        await _dbContext.TempApplicationUsers
            .Where(u => u.Email.ToLower() == tempUser.Email.ToLower() || u.UserName.ToLower() == tempUser.UserName.ToLower())
            .ExecuteDeleteAsync();

        return new ConfirmEmailResult { IsSuccess = true, User = applicationUser };
    }

    public async Task<string?> ResendVerificationMail(string userId)
    {
        var tempUser = await _dbContext.TempApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (tempUser == null)
            return null;

        if (tempUser.EmailConfirmCodeDate != null)
        {
            var sec = 60 - (int)(DateTime.UtcNow - tempUser.EmailConfirmCodeDate.Value).TotalSeconds;
            if (sec > 0)
            {
                return $"Повторно отправить код можно через {sec} секунд.";
            }
        }

        SetCode(tempUser);
        await _dbContext.SaveChangesAsync();

        await SendVerificationMail(tempUser.UserName, tempUser.Email, tempUser.EmailConfirmCode);
        return null;
    }

    private static void SetCode(TempApplicationUser? tempUser)
    {
        tempUser.EmailConfirmCodeDate = DateTime.UtcNow;
        tempUser.EmailConfirmCode = GetCode(8);
        tempUser.EmailConfirmCodeAttemt = 0;
        tempUser.IsEmailConfirmCodeBlock = false;
    }

    private Task SendVerificationMail(string userName, string email, string confirmCode)
    {
        const string title = "Подтверждение регистрации BOB.ID"; // todo BOB.ID -> вынести в настройку
        var body =
            $"Здравствуйте, {userName}!\r\nВаш код для подтверждения регистрации на сайте:\r\n{confirmCode}";

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
