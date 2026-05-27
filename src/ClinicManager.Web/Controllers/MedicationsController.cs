using ClinicManager.Web.DTOs;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Web.Controllers;

[Authorize(Roles = "Admin")]
public class MedicationsController : Controller
{
    private readonly IMedicationService _medicationService;

    public MedicationsController(IMedicationService medicationService)
    {
        _medicationService = medicationService;
    }

    // GET /Medications
    public async Task<IActionResult> Index()
    {
        var medications = await _medicationService.GetAllMedicationsAsync();
        return View(medications);
    }

    // GET /Medications/Create
    public IActionResult Create() => View(new CreateMedicationDto());

    // POST /Medications/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateMedicationDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        await _medicationService.CreateMedicationAsync(dto);
        TempData["Success"] = "Lek został dodany do katalogu.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Medications/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var medication = await _medicationService.GetMedicationByIdAsync(id);
        if (medication == null) return NotFound();

        return View(new CreateMedicationDto
        {
            Name = medication.Name,
            Description = medication.Description,
            Price = medication.Price
        });
    }

    // POST /Medications/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CreateMedicationDto dto)
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
    public async Task<IActionResult> Deactivate(int id)
    {
        await _medicationService.DeactivateMedicationAsync(id);
        TempData["Success"] = "Lek został dezaktywowany.";
        return RedirectToAction(nameof(Index));
    }
}