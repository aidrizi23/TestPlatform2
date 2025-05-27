using TestPlatform2.Repository;

namespace TestPlatform2.Services;

public class SubscriptionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionCleanupService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(1); // Run every hour

    public SubscriptionCleanupService(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscription cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWorkAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during subscription cleanup");
            }

            try
            {
                await Task.Delay(_period, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Subscription cleanup service stopped");
    }

    private async Task DoWorkAsync()
    {
        _logger.LogDebug("Starting subscription cleanup check");

        using var scope = _serviceProvider.CreateScope();
        var subscriptionRepository = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();

        try
        {
            var expiredUsers = await subscriptionRepository.GetExpiredSubscriptionsAsync();
            
            if (expiredUsers.Any())
            {
                _logger.LogInformation("Found {Count} expired subscriptions", expiredUsers.Count);
                await subscriptionRepository.RevokeExpiredSubscriptionsAsync();
                _logger.LogInformation("Successfully processed expired subscriptions");
            }
            else
            {
                _logger.LogDebug("No expired subscriptions found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired subscriptions");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscription cleanup service is stopping");
        await base.StopAsync(stoppingToken);
    }
}