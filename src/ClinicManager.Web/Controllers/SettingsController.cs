using ClinicManager.Web.DTOs;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Web.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IPatientService _patientService;

    public SettingsController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IPatientService patientService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _patientService = patientService;
    }

    public IActionResult Index()
    {
        return View();
    }

    // GET: /Settings/ChangePassword
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordDto());
    }

    // POST: /Settings/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound("Nie znaleziono użytkownika.");

        var changePasswordResult = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

        if (!changePasswordResult.Succeeded)
        {
            foreach (var error in changePasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(dto);
        }

        await _signInManager.RefreshSignInAsync(user);

        TempData["Success"] = "Twoje hasło zostało pomyślnie zmienione.";
        return RedirectToAction(nameof(Index));
    }
}