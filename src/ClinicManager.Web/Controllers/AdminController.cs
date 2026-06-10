using ClinicManager.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ClinicDbContext _context;

    public AdminController(UserManager<IdentityUser> userManager, ClinicDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var doctors = await _userManager.GetUsersInRoleAsync("Lekarz");

        var activeDoctors = doctors.Where(d => d.LockoutEnd == null || d.LockoutEnd <= DateTimeOffset.UtcNow);

        return View(activeDoctors);

    }

    [HttpGet]
    public IActionResult CreateDoctor()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDoctor(string email, string password)
    {
        if (!ModelState.IsValid) return View();

        var existing = await _userManager.FindByEmailAsync(email);

        if (existing != null)
        {
            if (await _userManager.IsInRoleAsync(existing, "Lekarz"))
            {
                var isLocked = await _userManager.IsLockedOutAsync(existing);
                if (isLocked)
                {
                    await _userManager.SetLockoutEndDateAsync(existing, null);
                    await _userManager.ResetAccessFailedCountAsync(existing);

                    var token = await _userManager.GeneratePasswordResetTokenAsync(existing);
                    await _userManager.ResetPasswordAsync(existing, token, password);

                    TempData["Success"] = $"Konto lekarza {email} zostało reaktywowane.";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Lekarz z tym adresem email już istnieje i jest aktywny.");
                    return View();
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Ten adres email jest już zajęty przez inne konto.");
                return View();
            }
        }

        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Lekarz");
            TempData["Success"] = $"Pomyślnie utworzono konto lekarza dla: {email}";
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateDoctor(string id)
    {
        var doctor = await _userManager.FindByIdAsync(id);
        if (doctor == null) return NotFound();

        if (await _userManager.IsInRoleAsync(doctor, "Admin"))
        {
            TempData["Error"] = "Nie można zablokować konta administratora!";
            return RedirectToAction(nameof(Index));
        }

        bool hasActiveVisits = await _context.Visits.AnyAsync(v =>
            v.DoctorId == id &&
            !v.IsDeleted &&
            ((int)v.Status == 0 || (int)v.Status == 1 || (int)v.Status == 2));

        if (hasActiveVisits)
        {
            TempData["Error"] = "Nie można zablokować tego lekarza! Posiada on aktywne lub nadchodzące wizyty pacjentów. Odwołaj lub przełóż wizyty przed dezaktywacją konta.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.SetLockoutEnabledAsync(doctor, true);
        await _userManager.SetLockoutEndDateAsync(doctor, DateTimeOffset.MaxValue);

        TempData["Success"] = $"Konto lekarza {doctor.UserName} zostało pomyślnie zablokowane.";
        return RedirectToAction(nameof(Index));
    }
}