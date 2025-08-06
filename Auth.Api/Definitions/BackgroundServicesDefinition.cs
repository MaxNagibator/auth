using Auth.Api.BackgroundServices;

namespace Auth.Api.Definitions;

public class BackgroundServicesDefinition : AppDefinition
{
    public override void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<EmailSenderBackgroundService>();
    }
}
