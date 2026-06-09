using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicManager.PerformanceTests;

/// <summary>
/// Generuje raport-indeksy.pdf (US#9) – opis indeksów, zapytania, oczekiwane plany PRZED/PO.
/// Screenshoty z SSMS dołącz ręcznie (Win+Shift+S) lub wklej do wersji końcowej w Word.
/// </summary>
public static class IndexReportPdfGenerator
{
    public static void Generate(string outputPath)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var generatedAt = DateTime.Now;

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("US#9 – Optymalizacja bazy danych (indeksy)")
                    .FontSize(18).Bold();
                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Item().Text($"Data: {generatedAt:dd.MM.yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Spacing(8);
                    col.Item().Text("Cel").FontSize(14).Bold();
                    col.Item().Text(
                        "Przyspieszenie wyszukiwania po PESEL (tabela Patients) oraz filtrowania wizyt lekarza " +
                        "(tabela Visits). Indeksy non-clustered zdefiniowane w EF Core Fluent API " +
                        "(src/ClinicManager.Web/Data/ClinicDbContext.cs).");
                    col.Spacing(8);
                    col.Item().Text("Indeksy").FontSize(14).Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn();
                            c.RelativeColumn(2);
                        });
                        HeaderCell(table, "Nazwa");
                        HeaderCell(table, "Tabela");
                        HeaderCell(table, "Kolumny / filtr");
                        DataCell(table, "IX_Patients_Pesel");
                        DataCell(table, "Patients");
                        DataCell(table, "Pesel (UNIQUE), filter [IsDeleted]=0");
                        DataCell(table, "IX_Visits_DoctorId_ScheduledDate");
                        DataCell(table, "Visits");
                        DataCell(table, "(DoctorId, ScheduledDate), filter [IsDeleted]=0");
                    });
                    col.Spacing(8);
                    col.Item().Text("Migracje EF Core").FontSize(14).Bold();
                    col.Item().Text("• 20260530095912_AddUniquePeselIndex");
                    col.Item().Text("• 20260531190905_AddDoctorVisitSearchIndex");
                });
                page.Footer().AlignRight().Text("ClinicManager – raport-indeksy.pdf");
            });

            document.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("Zapytanie 1 – PESEL (PatientService.PeselExistsAsync)").FontSize(14).Bold();
                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Item().Text("SQL (docs/sql/01-query-plan-przed-optymalizacja.sql):").Bold();
                    col.Item().Background(Colors.Grey.Lighten4).Padding(8).Text(
                        "DECLARE @Pesel NVARCHAR(11) = N'85010112345';\n" +
                        "SELECT CASE WHEN EXISTS (\n" +
                        "  SELECT 1 FROM Patients p\n" +
                        "  WHERE p.IsDeleted = 0 AND p.Pesel = @Pesel\n" +
                        ") THEN 1 ELSE 0 END;");
                    col.Spacing(8);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                        HeaderCell(table, "PRZED (bez indeksu)");
                        HeaderCell(table, "PO (z IX_Patients_Pesel)");
                        DataCell(table, "Clustered Index Scan");
                        DataCell(table, "Index Seek");
                        DataCell(table, "Więcej logical reads");
                        DataCell(table, "Mniej logical reads");
                    });
                    col.Spacing(8);
                    col.Item().Text("[Miejsce na screenshot Execution Plan – PRZED i PO]")
                        .Italic().FontColor(Colors.Grey.Darken1);
                });
            });

            document.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("Zapytanie 2 – lista pacjenta po PESEL").FontSize(14).Bold();
                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Item().Text("SQL:").Bold();
                    col.Item().Background(Colors.Grey.Lighten4).Padding(8).Text(
                        "SELECT p.Id, p.FirstName, p.LastName, p.Pesel\n" +
                        "FROM Patients p\n" +
                        "WHERE p.IsDeleted = 0 AND p.Pesel = @SearchPesel;");
                    col.Spacing(8);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                        HeaderCell(table, "PRZED");
                        HeaderCell(table, "PO");
                        DataCell(table, "Clustered Index Scan + filtr");
                        DataCell(table, "Index Seek na IX_Patients_Pesel");
                    });
                    col.Item().Text("[Miejsce na screenshot Execution Plan – PRZED i PO]")
                        .Italic().FontColor(Colors.Grey.Darken1);
                });
            });

            document.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("Zapytanie 3 – wizyty lekarza (VisitService.GetAllVisitsAsync)").FontSize(14).Bold();
                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Item().Text("SQL:").Bold();
                    col.Item().Background(Colors.Grey.Lighten4).Padding(8).Text(
                        "SELECT v.Id, v.PatientId, v.DoctorId, v.ScheduledDate, v.Status\n" +
                        "FROM Visits v\n" +
                        "WHERE v.IsDeleted = 0 AND v.DoctorId = @DoctorId\n" +
                        "ORDER BY v.ScheduledDate;");
                    col.Spacing(8);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                        HeaderCell(table, "PRZED");
                        HeaderCell(table, "PO");
                        DataCell(table, "Index Seek IX_Visits_DoctorId + Sort");
                        DataCell(table, "Index Seek IX_Visits_DoctorId_ScheduledDate (bez Sort)");
                    });
                    col.Item().Text("[Miejsce na screenshot Execution Plan – PRZED i PO]")
                        .Italic().FontColor(Colors.Grey.Darken1);
                });
            });

            document.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("Podsumowanie i instrukcja testu").FontSize(14).Bold();
                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Item().Text(
                        "Po dodaniu indeksów non-clustered plany wykonania zmieniają się ze Scan/Sort na Index Seek. " +
                        "STATISTICS IO (SET STATISTICS IO ON) pokazuje spadek logical reads.");
                    col.Spacing(8);
                    col.Item().Text("Kolejność w SSMS (docs/US9-PORADNIK-TESTOWANIA.md):").Bold();
                    col.Item().Text("1. docs/sql/00-usun-indeksy-przed-testem.sql");
                    col.Item().Text("2. docs/sql/01-query-plan-przed-optymalizacja.sql → screenshoty PRZED");
                    col.Item().Text("3. docs/sql/03-przywroc-indeksy.sql");
                    col.Item().Text("4. docs/sql/02-query-plan-po-optymalizacji.sql → screenshoty PO");
                    col.Spacing(8);
                    col.Item().Text("W SSMS: Ctrl+M (Include Actual Execution Plan) przed uruchomieniem zapytania.")
                        .Italic();
                });
                page.Footer().AlignRight().Text("ClinicManager – US#9");
            });
        }).GeneratePdf(outputPath);
    }

    private static void HeaderCell(TableDescriptor table, string text)
    {
        table.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(text).Bold();
    }

    private static void DataCell(TableDescriptor table, string text)
    {
        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(text);
    }
}
