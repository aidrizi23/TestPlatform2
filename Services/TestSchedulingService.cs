using Microsoft.EntityFrameworkCore;
using TestPlatform2.Data;

namespace TestPlatform2.Services;

public class TestSchedulingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TestSchedulingService> _logger;

    public TestSchedulingService(IServiceProvider serviceProvider, ILogger<TestSchedulingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Test Scheduling Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledTests();
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Check every minute
            }
            catch (TaskCanceledException)
            {
                // Normal cancellation during shutdown, no need to log as error
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled tests");
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes on error
                }
                catch (TaskCanceledException)
                {
                    // Cancelled during error delay, exit gracefully
                    break;
                }
            }
        }
    }

    private async Task ProcessScheduledTests()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;

        try
        {
            // Find tests that should be published (start time reached)
            var testsToPublish = await context.Tests
                .Where(t => t.IsScheduled && 
                           t.AutoPublish && 
                           t.Status == TestStatus.Scheduled &&
                           t.ScheduledStartDate.HasValue &&
                           t.ScheduledStartDate.Value <= now)
                .ToListAsync();

            foreach (var test in testsToPublish)
            {
                test.Status = TestStatus.Active;
                test.IsLocked = false; // Make the test available
                test.UpdatedAt = now;
                _logger.LogInformation("Auto-published test: {TestId} - {TestName}", test.Id, test.TestName);
            }

            // Find tests that should be closed (end time reached)
            var testsToClose = await context.Tests
                .Where(t => t.IsScheduled && 
                           t.AutoClose && 
                           t.Status == TestStatus.Active &&
                           t.ScheduledEndDate.HasValue &&
                           t.ScheduledEndDate.Value <= now)
                .ToListAsync();

            foreach (var test in testsToClose)
            {
                test.Status = TestStatus.Closed;
                test.IsLocked = true; // Lock the test
                test.UpdatedAt = now;
                _logger.LogInformation("Auto-closed test: {TestId} - {TestName}", test.Id, test.TestName);
            }

            if (testsToPublish.Any() || testsToClose.Any())
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Processed {PublishCount} tests for publishing and {CloseCount} tests for closing", 
                    testsToPublish.Count, testsToClose.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessScheduledTests");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Test Scheduling Service is stopping");
        await base.StopAsync(stoppingToken);
    }
}