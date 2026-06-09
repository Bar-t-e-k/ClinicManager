namespace ClinicManager.PerformanceTests;

/// <summary>
/// dotnet run --project tests/ClinicManager.PerformanceTests
///   (domyślnie: NBomber → nbomber-report.pdf)
/// dotnet run --project tests/ClinicManager.PerformanceTests -- index-report
/// dotnet run --project tests/ClinicManager.PerformanceTests -- sql-profiler
/// dotnet run --project tests/ClinicManager.PerformanceTests -- all-reports [baseUrl]
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "nbomber";

        return command switch
        {
            "index-report" => GenerateIndexReport(),
            "sql-profiler" => GenerateSqlProfilerReport(args),
            "all-reports" => GenerateAllReports(args),
            "nbomber" => RunNbomber(args),
            _ when command.StartsWith("http", StringComparison.OrdinalIgnoreCase) => RunNbomber(args),
            _ => RunNbomber(args)
        };
    }

    private static int GenerateIndexReport()
    {
        var path = ReportPaths.IndexReport;
        if (IndexReportScreenshotsPdfGenerator.TryGenerate(path))
        {
            Console.WriteLine($"Wygenerowano (ze screenshotami SSMS): {Path.GetFullPath(path)}");
            return 0;
        }

        IndexReportPdfGenerator.Generate(path);
        Console.WriteLine($"Wygenerowano (szablon tekstowy): {Path.GetFullPath(path)}");
        Console.WriteLine("Tip: wrzuć PNG do docs/us9-screenshots/ i uruchom ponownie.");
        return 0;
    }

    private static int GenerateSqlProfilerReport(string[] args)
    {
        var baseUrl = args.Length > 1 ? args[1] : "http://localhost:5215";
        var path = ReportPaths.SqlProfilerReport;
        if (SqlProfilerReportPdfGenerator.TryGenerate(path, baseUrl))
        {
            Console.WriteLine($"Wygenerowano (ze screenshotem EF log): {Path.GetFullPath(path)}");
            return 0;
        }

        SqlProfilerReportPdfGenerator.Generate(path, baseUrl);
        Console.WriteLine($"Wygenerowano (szablon): {Path.GetFullPath(path)}");
        Console.WriteLine("Tip: wrzuć PNG do docs/sql-profiler-screenshot.png i uruchom ponownie.");
        return 0;
    }

    private static int GenerateAllReports(string[] args)
    {
        var exit = GenerateIndexReport();
        if (exit != 0) return exit;

        exit = GenerateSqlProfilerReport(args);
        if (exit != 0) return exit;

        var baseUrl = args.Skip(1).FirstOrDefault(a => a.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            ?? "http://localhost:5215";

        try
        {
            return RunNbomber(new[] { "nbomber", baseUrl });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("NBomber pominięty (aplikacja niedostępna): " + ex.Message);
            Console.Error.WriteLine("Uruchom ClinicManager.Web i ponownie: dotnet run --project tests/ClinicManager.PerformanceTests -- nbomber");
            return 0;
        }
    }

    private static int RunNbomber(string[] args)
    {
        string baseUrl;
        string? pdfPath;

        if (args.Length > 0 && args[0].StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = args[0];
            pdfPath = args.Length > 1 ? args[1] : null;
        }
        else
        {
            baseUrl = args.Skip(1).FirstOrDefault(a => a.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                ?? "http://localhost:5215";
            pdfPath = args.Skip(1).FirstOrDefault(a => !a.StartsWith("http", StringComparison.OrdinalIgnoreCase) && a != "nbomber");
        }

        pdfPath ??= ReportPaths.NbomberReport;

        try
        {
            using var probe = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var health = probe.GetAsync($"{baseUrl.TrimEnd('/')}/api/visits/active").GetAwaiter().GetResult();
            if (!health.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"Endpoint zwrócił {(int)health.StatusCode}. Upewnij się, że aplikacja działa.");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Nie można połączyć z {baseUrl}. Uruchom najpierw: dotnet run --project src/ClinicManager.Web");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        VisitsLoadTest.Run(baseUrl, pdfPath);
        return 0;
    }
}
