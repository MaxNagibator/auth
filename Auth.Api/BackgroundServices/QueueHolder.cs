using Microsoft.AspNetCore.Identity.UI.Services;
using System.Collections.Concurrent;

namespace Auth.Api.BackgroundServices;

public class QueueHolder : IEmailSender
{
    public ConcurrentQueue<SendedMailMessage> MailMessages { get; } = new();

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        MailMessages.Enqueue(new(email, subject, htmlMessage));
        return Task.CompletedTask;
    }
}
