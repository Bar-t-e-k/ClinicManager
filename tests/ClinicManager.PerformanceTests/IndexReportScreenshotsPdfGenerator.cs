using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicManager.PerformanceTests;

/// <summary>
/// Składa raport-indeksy.pdf ze screenshotów SSMS (docs/us9-screenshots/).
/// </summary>
public static class IndexReportScreenshotsPdfGenerator
{
    public static bool TryGenerate(string outputPath)
    {
        var screenshotsDir = Path.Combine(ReportPaths.RepoRoot, "docs", "us9-screenshots");
        var files = new[]
        {
            ("01-pesel-exists-przed.png", "Zapytanie 1 – PESEL EXISTS – PRZED",
                "Clustered Index Scan na PK_Patients (100% kosztu). Skan całej tabeli."),
            ("02-lista-pesel-przed.png", "Zapytanie 2 – lista po PESEL – PRZED",
                "Clustered Index Scan na PK_Patients. Brak dedykowanego indeksu na Pesel."),
            ("03-wizyty-lekarza-przed.png", "Zapytanie 3 – wizyty lekarza – PRZED",
                "Clustered Index Scan (22%) + Sort (78%). Brak indeksu (DoctorId, ScheduledDate)."),
            ("04-pesel-exists-po.png", "Zapytanie 1 – PESEL EXISTS – PO",
                "Index Seek na IX_Patients_Pesel (100%). Precyzyjne wyszukiwanie zamiast skanu."),
            ("05-lista-pesel-po.png", "Zapytanie 2 – lista po PESEL – PO",
                "Przy małej liczbie wierszy optymalizator może nadal wybrać Scan; indeks IX_Patients_Pesel " +
                "jest używany wyraźnie w zapytaniu EXISTS (zap. 1). Przy większej tabeli → Index Seek."),
            ("06-wizyty-lekarza-po.png", "Zapytanie 3 – wizyty lekarza – PO",
                "Index Seek na IX_Visits_DoctorId_ScheduledDate (42%) + Key Lookup. Brak operacji Sort – " +
                "sortowanie obsługuje indeks złożony.")
        };

        foreach (var (file, _, _) in files)
        {
            if (!File.Exists(Path.Combine(screenshotsDir, file)))
                return false;
        }

        QuestPDF.Settings.License = LicenseType.Community;

        var tempPath = outputPath + ".tmp";
        if (File.Exists(tempPath)) File.Delete(tempPath);

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("US#9 – Raport optymalizacji indeksów").FontSize(18).Bold();
                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Item().Text($"Data: {DateTime.Now:dd.MM.yyyy HH:mm}");
                    col.Spacing(8);
                    col.Item().Text("Indeksy (EF Core Fluent API, ClinicDbContext.cs)").Bold();
                    col.Item().Text("• IX_Patients_Pesel – UNIQUE NONCLUSTERED, filter [IsDeleted]=0");
                    col.Item().Text("• IX_Visits_DoctorId_ScheduledDate – NONCLUSTERED, filter [IsDeleted]=0");
                    col.Spacing(8);
                    col.Item().Text("Podsumowanie").Bold();
                    col.Item().Text(
                        "PRZED: operacje Scan/Sort przy wyszukiwaniu po PESEL i wizytach lekarza. " +
                        "PO: Index Seek na dedykowanych indeksach – mniej odczytów, brak Sort przy wizytach lekarza.");
                });
                page.Footer().AlignRight().Text("ClinicManager – raport-indeksy.pdf");
            });

            foreach (var (file, title, comment) in files)
            {
                var path = Path.Combine(screenshotsDir, file);
                document.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text(title).FontSize(12).Bold();
                    page.Content().PaddingVertical(8).Column(col =>
                    {
                        col.Item().Text(comment).FontSize(9);
                        col.Item().PaddingTop(6).Image(path).FitArea();
                    });
                    page.Footer().AlignRight().Text(title);
                });
            }
        }).GeneratePdf(tempPath);

        FinalizePdf(tempPath, outputPath);

        return true;
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
            var outputDir = Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory();
            var alt = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(outputPath) + "-final.pdf");
            if (File.Exists(alt)) File.Delete(alt);
            File.Move(tempPath, alt);
            Console.WriteLine($"Plik {outputPath} jest otwarty – zapisano: {alt}");
        }
    }
}
