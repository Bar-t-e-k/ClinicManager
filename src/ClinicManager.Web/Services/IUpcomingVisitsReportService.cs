namespace ClinicManager.Web.Services;

public interface IUpcomingVisitsReportService
{
    /// <summary>
    /// Generuje PDF z wizytami na podany dzień i wysyła raport na e-mail administratora.
    /// </summary>
    Task GenerateAndSendAsync(DateTime targetDate, CancellationToken cancellationToken = default);
}
