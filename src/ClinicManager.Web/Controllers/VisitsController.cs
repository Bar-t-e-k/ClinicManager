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
    private readonly IProcedureService _procedureService;
    private readonly UserManager<IdentityUser> _userManager;

    public VisitsController(
        IVisitService visitService,
        IPatientService patientService,
        IMedicationService medicationService,
        IProcedureService procedureService,
        UserManager<IdentityUser> userManager)
    {
        _visitService = visitService;
        _patientService = patientService;
        _medicationService = medicationService;
        _procedureService = procedureService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Admin") || User.IsInRole("Rejestratorka"))
            return View(await _visitService.GetAllVisitsAsync());

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Forbid();

        if (User.IsInRole("Lekarz"))
            return View(await _visitService.GetAllVisitsAsync(user.Id));

        // Pacjent — tylko wizyty powiązane z jego rekordem pacjenta.
        var patientId = await _patientService.GetPatientIdByUserIdAsync(user.Id);
        var visits = patientId == null
            ? Enumerable.Empty<VisitDto>()
            : await _visitService.GetVisitsByPatientAsync(patientId.Value);

        return View(visits);
    }

    public async Task<IActionResult> Details(int id)
    {
        var visit = await _visitService.GetVisitDetailsAsync(id);
        if (visit == null) return NotFound();

        var accessDenied = await AuthorizeVisitViewAccessAsync(visit);
        if (accessDenied != null) return accessDenied;

        var medications = await _medicationService.GetAllMedicationsAsync();
        ViewBag.MedicationSelectList = medications
            .Where(m => m.IsActive)
            .Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = $"{m.Name} ({m.Price:C})"
            }).ToList();

        var procedures = await _procedureService.GetAllProceduresAsync();
        ViewBag.ProcedureSelectList = procedures
            .Where(p => p.IsActive)
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.Description} ({p.Cost:C})"
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
        if (!Enum.IsDefined(typeof(VisitStatus), status))
        {
            TempData["Error"] = "Nieprawidłowy status wizyty.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var updated = await _visitService.UpdateVisitStatusAsync(id, status);
        if (!updated)
            TempData["Error"] = "Nie udało się zaktualizować statusu wizyty.";
        else
            TempData["Success"] = "Status wizyty został zaktualizowany.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Lekarz")]
    public async Task<IActionResult> AddNote(int visitId, CreateClinicalNoteDto dto)
    {
        var accessDenied = await AuthorizeDoctorVisitAccessAsync(visitId);
        if (accessDenied != null) return accessDenied;

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
        var accessDenied = await AuthorizeDoctorVisitAccessAsync(visitId);
        if (accessDenied != null) return accessDenied;

        var result = await _visitService.DeleteClinicalNoteAsync(noteId, visitId);
        if (!result)
            TempData["Error"] = "Nie udało się usunąć notatki.";

        return RedirectToAction(nameof(Details), new { id = visitId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Lekarz")]
    public async Task<IActionResult> AddMedication(int visitId, AddMedicationToVisitDto dto)
    {
        var accessDenied = await AuthorizeDoctorVisitAccessAsync(visitId);
        if (accessDenied != null) return accessDenied;

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Nieprawidłowe dane leku.";
            return RedirectToAction(nameof(Details), new { id = visitId });
        }

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
        var accessDenied = await AuthorizeDoctorVisitAccessAsync(visitId);
        if (accessDenied != null) return accessDenied;

        await _visitService.RemoveMedicationAsync(visitMedicationId);
        return RedirectToAction(nameof(Details), new { id = visitId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Lekarz")]
    public async Task<IActionResult> AddProcedure(int visitId, AddProcedureToVisitDto dto)
    {
        var accessDenied = await AuthorizeDoctorVisitAccessAsync(visitId);
        if (accessDenied != null) return accessDenied;

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Nieprawidłowe dane procedury.";
            return RedirectToAction(nameof(Details), new { id = visitId });
        }

        var (success, error) = await _visitService.AddProcedureAsync(visitId, dto);
        if (!success)
            TempData["Error"] = error;

        return RedirectToAction(nameof(Details), new { id = visitId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Lekarz")]
    public async Task<IActionResult> RemoveProcedure(int visitProcedureId, int visitId)
    {
        var accessDenied = await AuthorizeDoctorVisitAccessAsync(visitId);
        if (accessDenied != null) return accessDenied;

        await _visitService.RemoveProcedureAsync(visitProcedureId);
        return RedirectToAction(nameof(Details), new { id = visitId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> Delete(int id)
    {
        await _visitService.CancelVisitAsync(id);
        TempData["Success"] = "Wizyta została anulowana.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Lekarz (bez roli Admin/Rejestratorka) może modyfikować tylko własne wizyty.
    /// </summary>
    private async Task<IActionResult?> AuthorizeDoctorVisitAccessAsync(int visitId)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Rejestratorka"))
            return null;

        // Każdy inny użytkownik (w tym bez przypisanej roli) ma dostęp tylko jako lekarz-właściciel wizyty.
        if (!User.IsInRole("Lekarz"))
            return Forbid();

        var doctorId = await _visitService.GetVisitDoctorIdAsync(visitId);
        if (doctorId == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null || doctorId != user.Id)
            return Forbid();

        return null;
    }

    /// <summary>
    /// Dostęp do podglądu wizyty: Admin/Rejestratorka — wszystko, Lekarz — własne wizyty,
    /// Pacjent — wyłącznie wizyty powiązane z jego rekordem pacjenta. Pozostali — brak dostępu.
    /// </summary>
    private async Task<IActionResult?> AuthorizeVisitViewAccessAsync(VisitDetailsDto visit)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Rejestratorka"))
            return null;

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Forbid();

        if (User.IsInRole("Lekarz"))
            return visit.DoctorId == user.Id ? null : Forbid();

        var patientId = await _patientService.GetPatientIdByUserIdAsync(user.Id);
        if (patientId != null && patientId.Value == visit.PatientId)
            return null;

        return Forbid();
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