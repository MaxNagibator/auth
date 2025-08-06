using Auth.Api.BackgroundServices;
using Auth.Business.Configs;

namespace Auth.Api.Definitions;

public class MailDefinition : AppDefinition
{
    public override void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(nameof(SmtpSettings)));
        builder.Services.Configure<EmailSenderSettings>(builder.Configuration.GetSection(nameof(EmailSenderSettings)));
        builder.Services.AddSingleton<IMailsService, MailsService>();
    }
}
