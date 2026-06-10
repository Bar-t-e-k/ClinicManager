using ClinicManager.Web.Configuration;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicManager.Web.Services;

public class UpcomingVisitsReportService : IUpcomingVisitsReportService
{
    private readonly IVisitService _visitService;
    private readonly IEmailSender _emailSender;
    private readonly UpcomingVisitsReportOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UpcomingVisitsReportService> _logger;

    public UpcomingVisitsReportService(
        IVisitService visitService,
        IEmailSender emailSender,
        IOptions<UpcomingVisitsReportOptions> options,
        IWebHostEnvironment environment,
        ILogger<UpcomingVisitsReportService> logger)
    {
        _visitService = visitService;
        _emailSender = emailSender;
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task GenerateAndSendAsync(DateTime targetDate, CancellationToken cancellationToken = default)
    {
        var visits = await _visitService.GetPlannedVisitsForDateAsync(targetDate);
        var pdfPath = ResolvePdfPath();

        Directory.CreateDirectory(Path.GetDirectoryName(pdfPath)!);
        GeneratePdf(pdfPath, targetDate, visits);

        var subject = $"Raport wizyt – {targetDate:dd.MM.yyyy}";
        var body = visits.Count == 0
            ? $"Brak zaplanowanych wizyt na dzień {targetDate:dd.MM.yyyy}."
            : $"W załączniku lista {visits.Count} zaplanowanych wizyt na dzień {targetDate:dd.MM.yyyy}.";

        await _emailSender.SendWithAttachmentAsync(
            _options.AdminEmail,
            subject,
            body,
            pdfPath,
            Path.GetFileName(pdfPath),
            cancellationToken);

        _logger.LogInformation(
            "Wysłano raport nadchodzących wizyt na {Date} ({Count} wizyt) do {Email}",
            targetDate.ToString("yyyy-MM-dd"),
            visits.Count,
            _options.AdminEmail);
    }

    private string ResolvePdfPath()
    {
        if (Path.IsPathRooted(_options.PdfOutputPath))
            return _options.PdfOutputPath;

        return Path.Combine(_environment.ContentRootPath, _options.PdfOutputPath);
    }

    private static void GeneratePdf(string filePath, DateTime targetDate, IReadOnlyList<DTOs.VisitDto> visits)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Margin(40);
                page.Header().Text($"Raport nadchodzących wizyt – {targetDate:dd.MM.yyyy}")
                    .FontSize(18).Bold();
                page.Content().PaddingVertical(12).Column(column =>
                {
                    if (visits.Count == 0)
                    {
                        column.Item().Text("Brak zaplanowanych wizyt na wybrany dzień.");
                        return;
                    }

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(90);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("Godzina").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("Pacjent").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("Lekarz").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("Status").Bold();
                        });

                        foreach (var visit in visits)
                        {
                            table.Cell().Padding(4).Text(visit.ScheduledDate.ToString("HH:mm"));
                            table.Cell().Padding(4).Text(visit.PatientFullName);
                            table.Cell().Padding(4).Text(visit.DoctorName);
                            table.Cell().Padding(4).Text(visit.Status);
                        }
                    });
                });
                page.Footer().AlignRight().Text($"Wygenerowano: {DateTime.Now:dd.MM.yyyy HH:mm}");
            });
        }).GeneratePdf(filePath);
    }
}