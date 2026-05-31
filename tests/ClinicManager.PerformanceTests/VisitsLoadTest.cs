using NBomber.CSharp;
using NBomber.Http;
using NBomber.Http.CSharp;

namespace ClinicManager.PerformanceTests;

/// <summary>
/// Test obciążeniowy GET /api/visits/active – 50 użytkowników równolegle, 100 żądań.
/// </summary>
public static class VisitsLoadTest
{
    public const string ScenarioName = "get_active_visits";
    public const int ConcurrentUsers = 50;
    public const int TotalRequests = 100;

    public static string Run(string baseUrl, string? pdfOutputPath = null)
    {
        baseUrl = baseUrl.TrimEnd('/');
        var endpoint = $"{baseUrl}/api/visits/active";
        pdfOutputPath ??= ResolveDefaultPdfPath();

        Console.WriteLine($"NBomber – test endpointu: GET {endpoint}");
        Console.WriteLine($"Scenariusz: {ConcurrentUsers} użytkowników równolegle, {TotalRequests} żądań łącznie.");
        Console.WriteLine();

        using var httpClient = new HttpClient();

        var scenario = Scenario.Create(ScenarioName, async context =>
        {
            var request = Http.CreateRequest("GET", endpoint);
            var response = await Http.Send(httpClient, request);
            return response;
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.IterationsForConstant(copies: ConcurrentUsers, iterations: TotalRequests));

        var runAt = DateTime.Now;

        var nodeStats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("nbomber-report")
            .Run();

        var scenarioStats = nodeStats.ScenarioStats[0];

        var okCount = scenarioStats.Ok.Request.Count;
        var failCount = scenarioStats.Fail.Request.Count;
        var allCount = scenarioStats.AllRequestCount;
        var rps = scenarioStats.Ok.Request.RPS;
        var latency = scenarioStats.Ok.Latency;

        Console.WriteLine("--- Wyniki ---");
        Console.WriteLine($"OK:      {okCount} ({scenarioStats.Ok.Request.Percent:F1}%)");
        Console.WriteLine($"Fail:    {failCount} ({scenarioStats.Fail.Request.Percent:F1}%)");
        Console.WriteLine($"Razem:   {allCount}");
        Console.WriteLine($"RPS:     {rps:F2}");
        Console.WriteLine($"Latency: mean={latency.MeanMs:F2} ms, p95={latency.Percent95:F2} ms");
        Console.WriteLine();

        NbomberReportPdfGenerator.Generate(pdfOutputPath, baseUrl, scenarioStats, runAt);
        Console.WriteLine($"Raport PDF: {Path.GetFullPath(pdfOutputPath)}");
        Console.WriteLine($"Raport HTML/TXT: {Path.GetFullPath("nbomber-report")}");

        return pdfOutputPath;
    }

    private static string ResolveDefaultPdfPath()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "ClinicManager.slnx")))
                return Path.Combine(dir, "nbomber-report.pdf");
            dir = Directory.GetParent(dir)?.FullName;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "nbomber-report.pdf");
    }
}
