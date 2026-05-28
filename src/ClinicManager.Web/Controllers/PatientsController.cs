using ClinicManager.Web.DTOs;
using ClinicManager.Web.Models;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Web.Controllers;

[Authorize]
public class PatientsController : Controller
{
    private readonly IPatientService _patientService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public PatientsController(IPatientService patientService, IWebHostEnvironment webHostEnvironment)
    {
        _patientService = patientService;
        _webHostEnvironment = webHostEnvironment;
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

    [Authorize(Roles = "Admin,Rejestratorka,Lekarz")]
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
    [Authorize(Roles = "Admin,Rejestratorka,Lekarz")]
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

    public async Task<IActionResult> Details(int id)
    {
        var patient = await _patientService.GetPatientDetailsAsync(id);
        if (patient == null) return NotFound();
        return View(patient);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Rejestratorka,Lekarz")]
    public async Task<IActionResult> UploadDocument(int patientId, IFormFile document)
    {
        if (document == null || document.Length == 0)
        {
            TempData["Error"] = "Wybierz plik przed wysłaniem.";
            return RedirectToAction(nameof(Details), new { id = patientId });
        }

        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        var fileExtension = Path.GetExtension(document.FileName).ToLower();

        if (!allowedExtensions.Contains(fileExtension))
        {
            TempData["Error"] = "Niedozwolony format pliku. Wgraj plik PDF, JPG lub PNG.";
            return RedirectToAction(nameof(Details), new { id = patientId });
        }

        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var absoluteFilePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(absoluteFilePath, FileMode.Create))
        {
            await document.CopyToAsync(fileStream);
        }

        var relativePath = $"/uploads/{uniqueFileName}";

        await _patientService.AddMedicalRecordAsync(patientId, document.FileName, relativePath);

        TempData["Success"] = "Dokument został wgrany.";
        return RedirectToAction(nameof(Details), new { id = patientId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Lekarz,Rejestratorka")] 
    public async Task<IActionResult> DeleteDocument(int recordId, int patientId)
    {
        var filePath = await _patientService.DeleteMedicalRecordAsync(recordId);

        if (filePath != null)
        {
            var absolutePath = Path.Combine(_webHostEnvironment.WebRootPath, filePath.TrimStart('/', '\\'));

            if (System.IO.File.Exists(absolutePath))
            {
                System.IO.File.Delete(absolutePath);
            }

            TempData["Success"] = "Dokument został pomyślnie usunięty.";
        }
        else
        {
            TempData["Error"] = "Nie znaleziono dokumentu do usunięcia.";
        }

        return RedirectToAction(nameof(Details), new { id = patientId });
    }
}