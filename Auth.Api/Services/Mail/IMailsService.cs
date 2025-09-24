namespace Auth.Api.Services.Mail;

public interface IMailsService
{
    Task SendAsync(SendedMailMessage mailMessage, CancellationToken cancellationToken = default);
}
