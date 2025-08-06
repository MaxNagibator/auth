namespace Auth.Api.Tests.TestTools.Entities;

/// <summary>
/// Пользователь.
/// </summary>
public class TestUser : TestObject
{
    public TestUser()
    {
        UserName = $"test_{Guid.NewGuid()}";
        Email = $"{UserName}@bobgroup.test.ru";
        Password = "123Qwerty9000!";
    }

    /// <summary>
    /// Идентификатор.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Логин.
    /// </summary>
    public string UserName { get; private set; }

    /// <summary>
    /// Email.
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// Пароль.
    /// </summary>
    public string Password { get; private set; }

    public override void LocalSave()
    {
        if (IsNew)
        {
            Environment.ApiClient.RegisterAsync(UserName, Email, Password).Wait();

            var dbUser = Environment.Context.Users
                .Single(x => x.UserName == UserName);

            Id = dbUser.Id;
        }
    }

    public TestUser SetPassword(string value)
    {
        Password = value;
        return this;
    }

    public TestUser SetUserName(string value)
    {
        UserName = value;
        return this;
    }

    public TestUser SetEmail(string? value)
    {
        Email = value;
        return this;
    }
}
