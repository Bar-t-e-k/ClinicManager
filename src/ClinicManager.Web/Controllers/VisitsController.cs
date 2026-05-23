using ClinicManager.Web.DTOs;
using ClinicManager.Web.Models;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicManager.Web.Controllers;

[Authorize]
public class VisitsController : Controller
{
    private readonly IVisitService _visitService;
    private readonly IPatientService _patientService;
    private readonly IMedicationService _medicationService;
    private readonly UserManager<IdentityUser> _userManager;

    public VisitsController(
        IVisitService visitService,
        IPatientService patientService,
        IMedicationService medicationService,
        UserManager<IdentityUser> userManager)
    {
        _visitService = visitService;
        _patientService = patientService;
        _medicationService = medicationService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        string? doctorFilter = null;
        if (User.IsInRole("Lekarz") && !User.IsInRole("Admin"))
        {
            var user = await _userManager.GetUserAsync(User);
            doctorFilter = user?.Id;
        }

        var visits = await _visitService.GetAllVisitsAsync(doctorFilter);
        return View(visits);
    }

    public async Task<IActionResult> Details(int id)
    {
        var visit = await _visitService.GetVisitDetailsAsync(id);
        if (visit == null) return NotFound();

        var medications = await _medicationService.GetAllMedicationsAsync();
        ViewBag.MedicationSelectList = medications
            .Where(m => m.IsActive)
            .Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = $"{m.Name} ({m.Price:C})"
            }).ToList();

        ViewBag.StatusSelectList = GetStatusSelectList(visit.Status);
        return View(visit);
    }

    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> Create()
    {
        await PopulateCreateViewBagsAsync();
        return View(new CreateVisitDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> Create(CreateVisitDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCreateViewBagsAsync();
            return View(dto);
        }

        var (success, error) = await _visitService.CreateVisitAsync(dto);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Wystąpił nieoczekiwany błąd.");
            await PopulateCreateViewBagsAsync();
            return View(dto);
        }

        TempData["Success"] = "Wizyta została zarejestrowana pomyślnie.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> UpdateStatus(int id, VisitStatus status)
    {
        await _visitService.UpdateVisitStatusAsync(id, status);
        TempData["Success"] = "Status wizyty został zaktualizowany.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Lekarz")]
    public async Task<IActionResult> AddNote(int visitId, CreateClinicalNoteDto dto)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Treść notatki jest nieprawidłowa.";
            return RedirectToAction(nameof(Details), new { id = visitId });
        }

        await _visitService.AddClinicalNoteAsync(visitId, dto);
        return RedirectToAction(nameof(Details), new { id = visitId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Lekarz")]
    public async Task<IActionResult> DeleteNote(int noteId, int visitId)
    {
        await _visitService.DeleteClinicalNoteAsync(noteId);
        return RedirectToAction(nameof(Details), new { id = visitId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Lekarz")]
    public async Task<IActionResult> AddMedication(int visitId, AddMedicationToVisitDto dto)
    {
        var (success, error) = await _visitService.AddMedicationAsync(visitId, dto);
        if (!success)
            TempData["Error"] = error;

        return RedirectToAction(nameof(Details), new { id = visitId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Lekarz")]
    public async Task<IActionResult> RemoveMedication(int visitMedicationId, int visitId)
    {
        await _visitService.RemoveMedicationAsync(visitMedicationId);
        return RedirectToAction(nameof(Details), new { id = visitId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> Delete(int id)
    {
        await _visitService.DeleteVisitAsync(id);
        TempData["Success"] = "Wizyta została anulowana.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCreateViewBagsAsync()
    {
        var patients = await _patientService.GetAllPatientsAsync();
        ViewBag.Patients = patients
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.LastName} {p.FirstName} (PESEL: {p.Pesel})"
            }).ToList();

        var doctors = await _userManager.GetUsersInRoleAsync("Lekarz");
        ViewBag.Doctors = doctors
            .Select(d => new SelectListItem
            {
                Value = d.Id,
                Text = d.Email ?? d.UserName ?? d.Id
            }).ToList();
    }

    private static List<SelectListItem> GetStatusSelectList(string currentStatus)
    {
        var statuses = new[]
        {
            ("Zaplanowana",  "Zaplanowana"),
            ("Potwierdzona", "Potwierdzona"),
            ("W trakcie",    "WTrakcie"),
            ("Zakończona",   "Zakonczona"),
            ("Anulowana",    "Anulowana"),
        };

        return statuses.Select(s => new SelectListItem
        {
            Value = s.Item2,
            Text = s.Item1,
            Selected = s.Item1 == currentStatus
        }).ToList();
    }
}