using ClinicManager.Web.Configuration;
using ClinicManager.Web.Services;
using Microsoft.Extensions.Options;

namespace ClinicManager.Web.BackgroundServices;

public class UpcomingVisitsReportBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly UpcomingVisitsReportOptions _options;
    private readonly ILogger<UpcomingVisitsReportBackgroundService> _logger;

    public UpcomingVisitsReportBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<UpcomingVisitsReportOptions> options,
        ILogger<UpcomingVisitsReportBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Uruchomiono {Service}. Interwał: {Minutes} min.",
            nameof(UpcomingVisitsReportBackgroundService),
            _options.IntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reportService = scope.ServiceProvider.GetRequiredService<IUpcomingVisitsReportService>();
                var tomorrow = DateTime.Today.AddDays(1);

                await reportService.GenerateAndSendAsync(tomorrow, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas generowania raportu nadchodzących wizyt.");
            }

            await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), stoppingToken);
        }
    }
}
