namespace Auth.Api.Services;

public sealed class PasswordResetCleanupService(
    IServiceProvider serviceProvider,
    ILogger<PasswordResetCleanupService> logger) : BackgroundService
{
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Сервис очистки кодов восстановления пароля запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                await PerformCleanupAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Произошла ошибка при очистке просроченных кодов восстановления пароля");
            }
        }

        logger.LogInformation("Сервис очистки кодов восстановления пароля остановлен");
    }

    private async Task PerformCleanupAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<ApplicationUserManager>();

        logger.LogInformation("Начало очистки просроченных кодов восстановления пароля");

        var expiredCodesCount = await userManager.CleanupExpiredVerificationCodesAsync();
        if (expiredCodesCount > 0)
        {
            logger.LogInformation("Очищено {Count} просроченных кодов верификации", expiredCodesCount);
        }

        var oldRecordsCount = await userManager.CleanupExpiredCodesAsync(TimeSpan.FromHours(24));
        if (oldRecordsCount > 0)
        {
            logger.LogInformation("Удалено {Count} старых записей восстановления пароля", oldRecordsCount);
        }

        if (expiredCodesCount == 0 && oldRecordsCount == 0)
        {
            logger.LogDebug("Нет просроченных кодов или старых записей для очистки");
        }
    }
}
