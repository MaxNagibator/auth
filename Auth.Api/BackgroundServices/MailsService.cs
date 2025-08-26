using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;

namespace Auth.Api.BackgroundServices;

public class EmailSenderSettings
{
    public TimeSpan ProcessingInterval { get; init; } = TimeSpan.FromSeconds(10);
    public int MaxRetries { get; init; } = 3;
    public double RetryBaseDelaySeconds { get; init; } = 5;
    public int MaxBatchSize { get; init; } = 100;
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount * 2;
}

public class SmtpSettings
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string UserName { get; init; }
    public required string Password { get; init; }
    public required bool EnableSSL { get; init; }
    public required string SenderEmail { get; init; }
}

public interface IMailsService
{
    Task SendAsync(SendedMailMessage mailMessage, CancellationToken cancellationToken = default);
}

public class MailsService(IOptions<SmtpSettings> options) : IMailsService
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

public readonly struct ValueStopwatch
{
    private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
    private readonly long _startTimestamp;

    private ValueStopwatch(long startTimestamp)
    {
        _startTimestamp = startTimestamp;
    }

    public static ValueStopwatch StartNew()
    {
        return new(Stopwatch.GetTimestamp());
    }

    public TimeSpan GetElapsedTime()
    {
        if (_startTimestamp == 0)
        {
            throw new InvalidOperationException("Stopwatch не был запущен");
        }

        var end = Stopwatch.GetTimestamp();
        var timestampDelta = end - _startTimestamp;
        var ticks = (long)(TimestampToTicks * timestampDelta);
        return new(ticks);
    }
}
