using ClinicManager.Web.DTOs;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Web.Controllers;

[Authorize]
public class MedicationsController : Controller
{
    private readonly IMedicationService _medicationService;

    public MedicationsController(IMedicationService medicationService)
    {
        _medicationService = medicationService;
    }

    // GET /Medications
    [Authorize(Roles = "Admin,Lekarz,Rejestratorka")]
    public async Task<IActionResult> Index()
    {
        var medications = await _medicationService.GetAllMedicationsAsync();
        return View(medications);
    }

    // GET /Medications/Create
    [Authorize(Roles = "Admin,Rejestratorka")]
    public IActionResult Create() => View(new CreateUpdateMedicationDto());

    // POST /Medications/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> Create(CreateUpdateMedicationDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        await _medicationService.CreateMedicationAsync(dto);
        TempData["Success"] = "Lek został dodany do katalogu.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Medications/Edit/5
    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> Edit(int id)
    {
        var medication = await _medicationService.GetMedicationByIdAsync(id);
        if (medication == null) return NotFound();

        return View(new CreateUpdateMedicationDto
        {
            Id = medication.Id,             
            Name = medication.Name,
            Description = medication.Description,
            Price = medication.Price,
            IsActive = medication.IsActive
        });
    }

    // POST /Medications/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> Edit(int id, CreateUpdateMedicationDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var success = await _medicationService.UpdateMedicationAsync(id, dto);
        if (!success) return NotFound();

        TempData["Success"] = "Dane leku zostały zaktualizowane.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Medications/Deactivate/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> Deactivate(int id)
    {
        await _medicationService.DeactivateMedicationAsync(id);
        TempData["Success"] = "Lek został dezaktywowany.";
        return RedirectToAction(nameof(Index));
    }
}