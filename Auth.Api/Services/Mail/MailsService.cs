using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Auth.Api.Services.Mail;

public sealed class MailsService(IOptions<SmtpSettings> options) : IMailsService
{
    private readonly SmtpSettings _settings = options.Value;

    public async Task SendAsync(SendedMailMessage mailMessage, CancellationToken cancellationToken = default)
    {
        using var client = Create();
        using var message = new MailMessage(_settings.SenderEmail, mailMessage.ReceiverEmail, mailMessage.Title, mailMessage.Body);
        await client.SendMailAsync(message, cancellationToken);
    }

    private SmtpClient Create()
    {
        return new(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSSL,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_settings.UserName, _settings.Password),
        };
    }
}
