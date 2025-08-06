using Auth.Business;

namespace Auth.Api.Definitions;

public class ContainerDefinition : AppDefinition
{
    public override void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<RequestEnvironment>();

        builder.Services.AddScoped<AccountsService>();
        builder.Services.AddScoped<AuthService>();

        builder.Services.AddSingleton<QueueHolder>();
    }
}
