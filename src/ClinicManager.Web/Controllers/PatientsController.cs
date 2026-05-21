using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Web.Controllers;

// [Authorize] // TODO: Do odkomentowania w przyszłości
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
}