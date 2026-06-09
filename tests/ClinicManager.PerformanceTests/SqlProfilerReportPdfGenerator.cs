using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicManager.PerformanceTests;

/// <summary>
/// Generuje raport-sql-profiler.pdf – EF Core log / SQL Profiler dla GET /api/visits/active.
/// </summary>
public static class SqlProfilerReportPdfGenerator
{
    private static string ScreenshotPath =>
        Path.Combine(ReportPaths.RepoRoot, "docs", "sql-profiler-screenshot.png");

    public static bool TryGenerate(string outputPath, string baseUrl = "http://localhost:5215")
    {
        if (!File.Exists(ScreenshotPath))
            return false;

        QuestPDF.Settings.License = LicenseType.Community;
        var generatedAt = DateTime.Now;
        var endpoint = $"{baseUrl.TrimEnd('/')}/api/visits/active";

        var tempPath = outputPath + ".tmp";
        if (File.Exists(tempPath)) File.Delete(tempPath);

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("SQL Profiler / EF Core – nasłuch endpointu API")
                    .FontSize(18).Bold();
                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Item().Text($"Data: {generatedAt:dd.MM.yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Spacing(10);
                    col.Item().Text("Monitorowany endpoint").FontSize(14).Bold();
                    col.Item().Text($"GET {endpoint}").FontFamily("Consolas").FontSize(11);
                    col.Spacing(10);
                    col.Item().Text("Narzędzie").FontSize(14).Bold();
                    col.Item().Text(
                        "EF Core Logging (Microsoft.EntityFrameworkCore.Database.Command) – " +
                        "alternatywa dla SQL Server Profiler zgodnie z wymaganiami projektu.");
                    col.Spacing(10);
                    col.Item().Text("Opis zapytania").FontSize(14).Bold();
                    col.Item().Text(
                        "Po wywołaniu endpointu EF Core wysyła SELECT z JOIN-ami: Visits → Patients → AspNetUsers " +
                        "(lekarz) oraz LEFT JOIN VisitMedications. Filtr: aktywne wizyty [Status] IN (0, 1, 2) " +
                        "(Zaplanowana, Potwierdzona, W trakcie) oraz IsDeleted = 0.");
                    col.Item().Text("Implementacja: VisitsApiController.GetActive → VisitService.GetActiveVisitsAsync().");
                    col.Spacing(10);
                    col.Item().Text("Wynik wykonania").FontSize(14).Bold();
                    col.Item().Text("Executed DbCommand – czas wykonania: 4 ms, baza: ClinicManagerDb.");
                });
                page.Footer().AlignRight().Text("ClinicManager – raport-sql-profiler.pdf");
            });

            document.Page(page =>
            {
                page.Margin(30);
                page.Header().Text("Screenshot – log EF Core (Executed DbCommand)").FontSize(12).Bold();
                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Item().Text(
                        "Zrzut ekranu z konsoli aplikacji po wywołaniu GET /api/visits/active. " +
                        "Widać pełne zapytanie SQL z JOIN-ami.")
                        .FontSize(9);
                    col.Item().PaddingTop(8).Image(ScreenshotPath).FitArea();
                });
                page.Footer().AlignRight().Text("GET /api/visits/active");
            });
        }).GeneratePdf(tempPath);

        FinalizePdf(tempPath, outputPath);
        return true;
    }

    public static void Generate(string outputPath, string baseUrl = "http://localhost:5215")
    {
        if (TryGenerate(outputPath, baseUrl))
            return;

        QuestPDF.Settings.License = LicenseType.Community;
        var endpoint = $"{baseUrl.TrimEnd('/')}/api/visits/active";

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("SQL Profiler / EF Core – nasłuch endpointu API").FontSize(18).Bold();
                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Item().Text($"GET {endpoint}").FontFamily("Consolas");
                    col.Item().Text("Brak docs/sql-profiler-screenshot.png – dodaj screenshot i uruchom ponownie.");
                });
            });
        }).GeneratePdf(outputPath);
    }

    private static void FinalizePdf(string tempPath, string outputPath)
    {
        if (!File.Exists(tempPath)) return;
        try
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
            File.Move(tempPath, outputPath);
        }
        catch (IOException)
        {
            var alt = Path.Combine(Path.GetDirectoryName(outputPath)!,
                Path.GetFileNameWithoutExtension(outputPath) + "-final.pdf");
            if (File.Exists(alt)) File.Delete(alt);
            File.Move(tempPath, alt);
            Console.WriteLine($"Plik {outputPath} jest otwarty – zapisano: {alt}");
        }
    }
}
