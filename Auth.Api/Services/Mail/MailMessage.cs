using System.Diagnostics.CodeAnalysis;

namespace Auth.Api.BackgroundServices.Mail;

/// <summary>
/// Электронное письмо.
/// </summary>
/// <param name="email">Адрес электронной почты получателя.</param>
/// <param name="title">Заголовок письма.</param>
/// <param name="body">Тело письма.</param>
[method: SetsRequiredMembers]
public class SendedMailMessage(string email, string title, string body)
{
    /// <summary>
    /// Идентификатор.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Адрес электронной почты получателя.
    /// </summary>
    public required string ReceiverEmail { get; init; } = email;

    /// <summary>
    /// Заголовок письма.
    /// </summary>
    public required string Title { get; init; } = title;

    /// <summary>
    /// Тело письма.
    /// </summary>
    public required string Body { get; init; } = body;

    /// <summary>
    /// Количество попыток отправки письма.
    /// </summary>
    public int RetryCount { get; set; }
}
