using NBomber.Contracts.Stats;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicManager.PerformanceTests;

public static class NbomberReportPdfGenerator
{
    public static void Generate(string outputPath, string baseUrl, ScenarioStats stats, DateTime runAt)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var ok = stats.Ok;
        var fail = stats.Fail;
        var latency = ok.Latency;
        var allCount = stats.AllRequestCount;

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("Raport testu wydajności NBomber")
                    .FontSize(20).Bold();
                page.Content().PaddingVertical(16).Column(column =>
                {
                    column.Item().Text($"Data testu: {runAt:dd.MM.yyyy HH:mm:ss}");
                    column.Item().Text($"Endpoint: GET {baseUrl.TrimEnd('/')}/api/visits/active");
                    column.Item().Text($"Scenariusz: {stats.ScenarioName}").Bold();
                    column.Spacing(8);

                    column.Item().Text("Parametry scenariusza").FontSize(14).Bold();
                    column.Item().Text("• 50 równoległych użytkowników (IterationsForConstant)");
                    column.Item().Text("• 100 żądań łącznie (IterationsForConstant)");
                    column.Spacing(8);

                    column.Item().Text("Podsumowanie żądań").FontSize(14).Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn();
                        });
                        AddRow(table, "Łączna liczba żądań", allCount.ToString());
                        AddRow(table, "Sukces (OK)", $"{ok.Request.Count} ({ok.Request.Percent:F1}%)");
                        AddRow(table, "Błędy (Fail)", $"{fail.Request.Count} ({fail.Request.Percent:F1}%)");
                        AddRow(table, "Throughput (RPS)", $"{ok.Request.RPS:F2}");
                    });
                    column.Spacing(8);

                    column.Item().Text("Czasy odpowiedzi (ms) – sukces").FontSize(14).Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn();
                        });
                        AddRow(table, "Min", $"{latency.MinMs:F2}");
                        AddRow(table, "Średnia (Mean)", $"{latency.MeanMs:F2}");
                        AddRow(table, "Max", $"{latency.MaxMs:F2}");
                        AddRow(table, "P50", $"{latency.Percent50:F2}");
                        AddRow(table, "P75", $"{latency.Percent75:F2}");
                        AddRow(table, "P95", $"{latency.Percent95:F2}");
                        AddRow(table, "P99", $"{latency.Percent99:F2}");
                    });

                    if (fail.Request.Count > 0)
                    {
                        column.Spacing(8);
                        column.Item().Text("Uwaga: wystąpiły nieudane żądania – sprawdź logi aplikacji i dostępność endpointu.")
                            .FontColor(Colors.Red.Medium);
                    }
                });
                page.Footer().AlignRight().Text("ClinicManager – US#8 NBomber");
            });
        }).GeneratePdf(outputPath);
    }

    private static void AddRow(TableDescriptor table, string label, string value)
    {
        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(label);
        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(value);
    }
}
