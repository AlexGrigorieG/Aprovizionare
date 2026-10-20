using Microsoft.Extensions.Options;

namespace Aprovizionare.Api.Services;

public class DailyAlertsJob(IServiceScopeFactory scopeFactory, IOptions<AlertOptions> options, ILogger<DailyAlertsJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextRun(DateTime.Now, options.Value.DailyAt);
            logger.LogInformation("Următoarele alerte pe email pleacă la {NextRun:dd.MM.yyyy HH:mm}", DateTime.Now + delay);

            await Task.Delay(delay, stoppingToken);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var sent = await scope.ServiceProvider.GetRequiredService<AlertService>().SendAsync(stoppingToken);
                logger.LogInformation("Alerte trimise: {Count} emailuri", sent);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Trimiterea alertelor pe email a eșuat");
            }
        }
    }

    public static TimeSpan TimeUntilNextRun(DateTime now, TimeOnly runAt)
    {
        var next = now.Date + runAt.ToTimeSpan();
        if (next <= now)
            next = next.AddDays(1);

        return next - now;
    }
}
