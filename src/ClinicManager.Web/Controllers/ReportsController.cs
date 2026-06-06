using ClinicManager.Web.Data;
using ClinicManager.Web.Models;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Web.Controllers;

[Authorize]
[Route("Reports")]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly ClinicDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public ReportsController(IReportService reportService, ClinicDbContext context, UserManager<IdentityUser> userManager)
    {
        _reportService = reportService;
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("DownloadPatientMonthlyReport")]
    public async Task<IActionResult> DownloadPatientMonthlyReport(int patientId, int year, int month)
    {
        var currentUserId = _userManager.GetUserId(User);
        var isStaff = User.IsInRole("Lekarz") || User.IsInRole("Admin") || User.IsInRole("Recepcjonistka");

        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null) return NotFound("Nie znaleziono pacjenta");

        if (!isStaff && patient.UserId != currentUserId)
        {
            return Forbid();
        }

        try
        {
            var pdfBytes = await _reportService.GeneratePatientMonthlyCostReportAsync(patientId, year, month);
            var fileName = $"Raport_Kosztow_Pacjent_{patientId}_{year}_{month:D2}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("DownloadDoctorMonthlyReport")]
    public async Task<IActionResult> DownloadDoctorMonthlyReport(string doctorId, int year, int month)
    {
        var currentUserId = _userManager.GetUserId(User);

        if (currentUserId != doctorId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        try
        {
            var pdfBytes = await _reportService.GenerateDoctorMonthlyCostReportAsync(doctorId, year, month);
            return File(pdfBytes, "application/pdf", $"Raport_Lekarz_{year}_{month:D2}.pdf");
        }
        catch (ArgumentException ex) { return NotFound(ex.Message); }
    }

    [HttpGet("DownloadMyPatientReport")]
    public async Task<IActionResult> DownloadMyPatientReport(int year, int month)
    {
        var currentUserId = _userManager.GetUserId(User);

        if (currentUserId == null)
        {
            return Challenge(); 
        }

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == currentUserId && !p.IsDeleted);

        if (patient == null)
        {
            return NotFound("Nie znaleziono profilu pacjenta powiązanego z Twoim kontem.");
        }

        try
        {
            var pdfBytes = await _reportService.GeneratePatientMonthlyCostReportAsync(patient.Id, year, month);
            var fileName = $"Moj_Raport_Kosztow_{year}_{month:D2}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }
}