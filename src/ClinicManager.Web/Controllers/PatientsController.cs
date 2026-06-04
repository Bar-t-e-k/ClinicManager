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
    private readonly IFileStorageService _fileStorageService;

    public PatientsController(IPatientService patientService, IFileStorageService fileStorageService)
    {
        _patientService = patientService;
        _fileStorageService = fileStorageService;
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

        if (dto.AvatarFile != null)
        {
            dto.AvatarUrl = await _fileStorageService.SaveFileAsync(dto.AvatarFile, "uploads/patients");
        }

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
            InsuranceNumber = patient.InsuranceNumber,
            AvatarUrl = patient.AvatarUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Rejestratorka,Lekarz")]
    public async Task<IActionResult> Edit(int id, CreateUpdatePatientDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var existingPatient = await _patientService.GetPatientByIdAsync(id);
        if (existingPatient == null) return NotFound();

        if (dto.AvatarFile != null)
        {
            if (!string.IsNullOrEmpty(existingPatient.AvatarUrl))
            {
                _fileStorageService.DeleteFile(existingPatient.AvatarUrl);
            }

            dto.AvatarUrl = await _fileStorageService.SaveFileAsync(dto.AvatarFile, "uploads/patients");
        }
        else
        {
            dto.AvatarUrl = existingPatient.AvatarUrl;
        }

        await _patientService.UpdatePatientAsync(id, dto);
        TempData["Success"] = "Dane pacjenta zostały zaktualizowane.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _patientService.DeletePatientAsync(id);
        if (!success) return NotFound();

        TempData["Success"] = "Pacjent oraz powiązane pliki zostały pomyślnie usunięte.";
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

        try
        {
            var relativePath = await _fileStorageService.SaveFileAsync(document, "uploads");

            if (relativePath != null)
            {
                bool isSuccess = await _patientService.AddMedicalRecordAsync(patientId, document.FileName, relativePath);

                if (isSuccess)
                {
                    TempData["Success"] = "Dokument został wgrany.";
                }
                else
                {
                    _fileStorageService.DeleteFile(relativePath);
                    TempData["Error"] = "Nie znaleziono pacjenta. Plik nie został przypisany.";
                }
            }
        }
        catch (Exception)
        {
            TempData["Error"] = "Wystąpił błąd po stronie serwera. Plik nie został wgrany.";
        }

        return RedirectToAction(nameof(Details), new { id = patientId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Lekarz,Rejestratorka")]
    public async Task<IActionResult> DeleteDocument(int recordId, int patientId)
    {
        var filePath = await _patientService.DeleteMedicalRecordAsync(recordId, patientId);

        if (filePath != null)
        {
            _fileStorageService.DeleteFile(filePath);

            TempData["Success"] = "Dokument został pomyślnie usunięty.";
        }
        else
        {
            TempData["Error"] = "Nie znaleziono dokumentu do usunięcia lub brak uprawnień.";
        }

        return RedirectToAction(nameof(Details), new { id = patientId });
    }
}