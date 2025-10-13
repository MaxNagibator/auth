namespace Auth.Api.Services.Mail;

public sealed class SmtpSettings
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string UserName { get; init; }
    public required string Password { get; init; }
    public required bool EnableSSL { get; init; }
    public required string SenderEmail { get; init; }
}
