#pragma warning disable CA1065
using Auth.Data.Entities;

namespace Auth.Business;

public class RequestEnvironment
{
    private int? _userId;

    // TODO: Спорное решение. Возможно лучше сделать через метод
    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    /// <exception cref="BusinessException">анонимный пользователь.</exception>
    public int UserId
    {
        get => _userId ?? throw new BusinessException("Извините, но идентификатор пользователя не указан.");
        set => _userId = value;
    }

    public ApplicationUser? AuthUser { get; set; }
}
