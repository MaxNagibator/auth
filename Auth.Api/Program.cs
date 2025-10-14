using Auth.Api;
using Auth.Api.Data;
using Auth.Api.Services;
using Auth.Api.Services.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Quartz;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString(nameof(ApplicationDbContext)));
    options.LogTo(Console.WriteLine);
    options.UseOpenIddict();
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 12;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.SignIn.RequireConfirmedEmail = true;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

builder.Services.AddQuartz(options =>
{
    options.UseMicrosoftDependencyInjectionJobFactory();
    options.UseSimpleTypeLoader();
    options.UseInMemoryStore();
});

builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>();
        options.UseQuartz();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("connect/authorize")
            .SetLogoutEndpointUris("connect/logout")
            .SetTokenEndpointUris("connect/token")
            .SetUserinfoEndpointUris("connect/userinfo");

        options.RegisterScopes(OpenIddictConstants.Scopes.Email, OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Roles);

        options.AllowAuthorizationCodeFlow();

        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableLogoutEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableUserinfoEndpointPassthrough()
            .EnableStatusCodePagesIntegration();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<PasswordResetCleanupService>();
builder.Services.AddTransient<ApplicationUserManager>();

builder.Services.AddSingleton<IMailsService, MailsService>();
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(nameof(SmtpSettings)));
builder.Services.Configure<EmailSenderSettings>(builder.Configuration.GetSection(nameof(EmailSenderSettings)));
builder.Services.Configure<ServiceInfoSettings>(builder.Configuration.GetSection(nameof(ServiceInfoSettings)));

builder.Services.AddSingleton<EmailSenderBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EmailSenderBackgroundService>());
builder.Services.AddSingleton<IEmailSender>(x => x.GetRequiredService<EmailSenderBackgroundService>());

var app = builder.Build();

//  ef database update --project Auth.Api\Auth.Api.csproj --startup-project Auth.Api\Auth.Api.csproj --context Auth.Api.Data.ApplicationDbContext --configuration Debug --verbose 20250814092818_Initial

var automigrate = app.Configuration["AUTO_MIGRATE"];
if (automigrate?.ToLower(CultureInfo.InvariantCulture) == "true" || automigrate == "1")
{
    using var scope = app.Services.CreateScope();
    using var applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    applicationDbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseStatusCodePagesWithReExecute("~/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultControllerRoute();
app.MapRazorPages();

app.Run();
