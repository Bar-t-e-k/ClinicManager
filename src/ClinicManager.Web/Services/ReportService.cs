using ClinicManager.Web.Data;
using ClinicManager.Web.DTOs;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicManager.Web.Services;

public class ReportService : IReportService
{
    private readonly ClinicDbContext _context;

    public ReportService(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> GeneratePatientMonthlyCostReportAsync(int patientId, int year, int month)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == patientId);
        if (patient == null) throw new ArgumentException("Nie znaleziono pacjenta");

        var personName = $"{patient.FirstName} {patient.LastName} (PESEL: {patient.Pesel})";

        var query = _context.Visits
            .Where(v => v.PatientId == patientId &&
                        v.ScheduledDate.Year == year &&
                        v.ScheduledDate.Month == month &&
                        !v.IsDeleted);

        var data = await AggregateCostsAsync(query);

        var reportDto = new MonthlyReportDto
        {
            ReportTitle = "Miesięczny Raport Kosztów Pacjenta",
            PersonName = personName,
            Year = year,
            Month = month,
            DailySummaries = data
        };

        return GeneratePdf(reportDto);
    }

    public async Task<byte[]> GenerateDoctorMonthlyCostReportAsync(string doctorId, int year, int month)
    {
        var doctor = await _context.Users.FirstOrDefaultAsync(u => u.Id == doctorId);
        if (doctor == null) throw new ArgumentException("Nie znaleziono lekarza");

        var personName = $"Dr {doctor.UserName}";

        var query = _context.Visits
            .Where(v => v.DoctorId == doctorId &&
                        v.ScheduledDate.Year == year &&
                        v.ScheduledDate.Month == month &&
                        !v.IsDeleted);

        var data = await AggregateCostsAsync(query);

        var reportDto = new MonthlyReportDto
        {
            ReportTitle = "Miesięczny Raport Kosztów Świadczeń Lekarza",
            PersonName = personName,
            Year = year,
            Month = month,
            DailySummaries = data
        };

        return GeneratePdf(reportDto);
    }

    private async Task<List<DailyCostSummaryDto>> AggregateCostsAsync(IQueryable<Models.Visit> query)
    {
        var dailyData = await query
            .Select(v => new
            {
                Date = v.ScheduledDate.Date,
                MedicationsCost = v.VisitMedications.Sum(m => m.Quantity * m.UnitPrice),
                ProceduresCost = v.VisitProcedures.Sum(p => p.Quantity * p.UnitCost),
                VisitTotalCost = v.TotalCost 
            })
            .GroupBy(x => x.Date)
            .Select(g => new DailyCostSummaryDto
            {
                Date = g.Key,
                MedicationsCost = g.Sum(x => x.MedicationsCost),
                ProceduresCost = g.Sum(x => x.ProceduresCost),
                TotalCost = g.Sum(x => x.VisitTotalCost)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        return dailyData;
    }

    private byte[] GeneratePdf(MonthlyReportDto report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Header().Element(compose => ComposeHeader(compose, report));
                page.Content().Element(compose => ComposeContent(compose, report));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Strona ");
                    x.CurrentPageNumber();
                    x.Span(" z ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, MonthlyReportDto report)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(report.ReportTitle).FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text($"Dotyczy: {report.PersonName}");
                column.Item().Text($"Okres: {report.Month:D2}/{report.Year}");
                column.Item().Text($"Wygenerowano: {report.GeneratedAt:yyyy-MM-dd HH:mm}");
            });
        });
    }

    private void ComposeContent(IContainer container, MonthlyReportDto report)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(5);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().BorderBottom(1).PaddingBottom(5).Text("Data wizyt").SemiBold();
                    header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("Koszty leków").SemiBold();
                    header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("Koszty procedur").SemiBold();
                    header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("Koszt całkowity").SemiBold();
                });

                foreach (var item in report.DailySummaries)
                {
                    table.Cell().PaddingVertical(5).Text(item.Date.ToString("dd.MM.yyyy"));
                    table.Cell().PaddingVertical(5).AlignRight().Text($"{item.MedicationsCost:C}");
                    table.Cell().PaddingVertical(5).AlignRight().Text($"{item.ProceduresCost:C}");
                    table.Cell().PaddingVertical(5).AlignRight().Text($"{item.TotalCost:C}");
                }

                table.Footer(footer =>
                {
                    footer.Cell().BorderTop(1).PaddingTop(5).Text("SUMA:").SemiBold();
                    footer.Cell().BorderTop(1).PaddingTop(5).AlignRight().Text($"{report.TotalMedicationsCost:C}").SemiBold();
                    footer.Cell().BorderTop(1).PaddingTop(5).AlignRight().Text($"{report.TotalProceduresCost:C}").SemiBold();
                    footer.Cell().BorderTop(1).PaddingTop(5).AlignRight().Text($"{report.TotalCost:C}").SemiBold();
                });
            });
        });
    }
}