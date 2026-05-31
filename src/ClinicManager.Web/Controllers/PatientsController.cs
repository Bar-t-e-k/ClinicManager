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

        var (patientId, error) = await _patientService.CreatePatientAsync(dto);
        if (patientId == null)
        {
            ModelState.AddModelError(nameof(dto.Pesel), error ?? "Nie udało się dodać pacjenta.");
            return View(dto);
        }

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

        var (success, error) = await _patientService.UpdatePatientAsync(id, dto);
        if (!success && error == null) return NotFound();
        if (!success)
        {
            ModelState.AddModelError(nameof(dto.Pesel), error!);
            return View(dto);
        }

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

        try
        {
            bool isSuccess = await _patientService.AddMedicalRecordAsync(patientId, document.FileName, relativePath);

            if (isSuccess)
            {
                TempData["Success"] = "Dokument został wgrany.";
            }
            else
            {
                if (System.IO.File.Exists(absoluteFilePath))
                {
                    System.IO.File.Delete(absoluteFilePath);
                }
                TempData["Error"] = "Nie znaleziono pacjenta. Plik nie został przypisany.";
            }
        }
        catch (Exception)
        {
            if (System.IO.File.Exists(absoluteFilePath))
            {
                System.IO.File.Delete(absoluteFilePath);
            }

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
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");

            var absolutePath = Path.GetFullPath(Path.Combine(_webHostEnvironment.WebRootPath, filePath.TrimStart('/', '\\')));

            if (absolutePath.StartsWith(uploadsFolder, StringComparison.OrdinalIgnoreCase))
            {
                if (System.IO.File.Exists(absolutePath))
                {
                    System.IO.File.Delete(absolutePath);
                }
            }

            TempData["Success"] = "Dokument został pomyślnie usunięty.";
        }
        else
        {
            TempData["Error"] = "Nie znaleziono dokumentu do usunięcia lub brak uprawnień.";
        }

        return RedirectToAction(nameof(Details), new { id = patientId });
    }
}