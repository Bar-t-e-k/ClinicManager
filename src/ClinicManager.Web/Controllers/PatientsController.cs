using ClinicManager.Web.DTOs;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Web.Controllers;

[Authorize]
public class PatientsController : Controller
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    public async Task<IActionResult> Index(string? searchTerm)
    {
        ViewData["CurrentFilter"] = searchTerm;
        var patients = await _patientService.GetAllPatientsAsync(searchTerm);
        return View(patients);
    }

    [Authorize(Roles = "Admin,Rejestratorka")]
    public IActionResult Create() => View(new CreateUpdatePatientDto());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> Create(CreateUpdatePatientDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _patientService.CreatePatientAsync(dto);
        TempData["Success"] = "Pacjent został dodany.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var patient = await _patientService.GetPatientByIdAsync(id);
        if (patient == null) return NotFound();

        return View(new CreateUpdatePatientDto
        {
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            Pesel = patient.Pesel,
            InsuranceNumber = patient.InsuranceNumber
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CreateUpdatePatientDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var success = await _patientService.UpdatePatientAsync(id, dto);
        if (!success) return NotFound();
        TempData["Success"] = "Dane pacjenta zostały zaktualizowane.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _patientService.DeletePatientAsync(id);
        TempData["Success"] = "Pacjent został usunięty.";
        return RedirectToAction(nameof(Index));
    }
}