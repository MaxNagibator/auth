using Auth.Api.Middlewares;

namespace Auth.Api.Definitions;

public class MiddlewareDefinition : AppDefinition
{
    public override int ApplicationOrderIndex => 1;

    public override void ConfigureApplication(WebApplication app)
    {
        app.UseMiddleware<AuthMiddleware>();
    }
}
