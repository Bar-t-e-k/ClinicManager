using ClinicManager.Web.DTOs;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Web.Controllers;

[Authorize]
public class ProceduresController : Controller
{
    private readonly IProcedureService _procedureService;

    public ProceduresController(IProcedureService procedureService)
    {
        _procedureService = procedureService;
    }

    // GET /Procedures
    [Authorize(Roles = "Admin,Lekarz,Rejestratorka")]
    public async Task<IActionResult> Index()
    {
        var procedures = await _procedureService.GetAllProceduresAsync();
        return View(procedures);
    }

    // GET /Procedures/Create
    [Authorize(Roles = "Admin,Rejestratorka")]
    public IActionResult Create() => View(new CreateUpdateProcedureDto());

    // POST /Procedures/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> Create(CreateUpdateProcedureDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        await _procedureService.CreateProcedureAsync(dto);
        TempData["Success"] = "Procedura została dodana do katalogu.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Procedures/Edit/5
    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> Edit(int id)
    {
        var procedure = await _procedureService.GetProcedureByIdAsync(id);
        if (procedure == null) return NotFound();

        return View(procedure);
    }

    // POST /Procedures/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> Edit(int id, CreateUpdateProcedureDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var success = await _procedureService.UpdateProcedureAsync(id, dto);
        if (!success) return NotFound();

        TempData["Success"] = "Dane procedury zostały zaktualizowane.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Procedures/Deactivate/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> Deactivate(int id)
    {
        await _procedureService.DeactivateProcedureAsync(id);
        TempData["Success"] = "Procedura została dezaktywowana.";
        return RedirectToAction(nameof(Index));
    }
}