namespace ClinicManager.PerformanceTests;

/// <summary>
/// Uruchomienie: dotnet run --project tests/ClinicManager.PerformanceTests
/// (aplikacja ClinicManager.Web musi działać na podanym adresie)
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        var baseUrl = args.FirstOrDefault() ?? "http://localhost:5215";
        var pdfPath = args.Length > 1 ? args[1] : null;

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
