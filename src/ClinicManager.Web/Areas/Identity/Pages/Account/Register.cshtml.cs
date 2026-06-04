using System.ComponentModel.DataAnnotations;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClinicManager.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserStore<IdentityUser> _userStore;
    private readonly IUserEmailStore<IdentityUser> _emailStore;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IPatientService _patientService;

    public RegisterModel(
        UserManager<IdentityUser> userManager,
        IUserStore<IdentityUser> userStore,
        SignInManager<IdentityUser> signInManager,
        ILogger<RegisterModel> logger,
        IPatientService patientService)
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore();
        _signInManager = signInManager;
        _logger = logger;
        _patientService = patientService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Imię jest wymagane.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Imię musi mieć od 2 do 100 znaków.")]
        [Display(Name = "Imię")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nazwisko jest wymagane.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Nazwisko musi mieć od 2 do 100 znaków.")]
        [Display(Name = "Nazwisko")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "PESEL jest wymagany.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "PESEL musi składać się z dokładnie 11 cyfr.")]
        [Display(Name = "PESEL")]
        public string Pesel { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy adres e-mail.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        [StringLength(100, ErrorMessage = "Hasło musi mieć co najmniej {2} znaków.", MinimumLength = 4)]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź hasło")]
        [Compare(nameof(Password), ErrorMessage = "Hasła nie są zgodne.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return Page();

        var user = CreateUser();
        await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
        await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
        user.EmailConfirmed = true;

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        var (linked, linkError) = await _patientService.LinkOrCreatePatientForUserAsync(
            user.Id, Input.Pesel, Input.FirstName, Input.LastName);

        if (!linked)
        {
            // Bez powiązania z pacjentem konto nie ma sensu — wycofujemy je, aby nie zostało osierocone.
            await _userManager.DeleteAsync(user);
            ModelState.AddModelError(string.Empty, linkError ?? "Nie udało się powiązać konta z pacjentem.");
            return Page();
        }

        _logger.LogInformation("Nowy pacjent utworzył konto i został powiązany z rekordem pacjenta.");
        await _signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(returnUrl);
    }

    private static IdentityUser CreateUser()
    {
        try
        {
            return Activator.CreateInstance<IdentityUser>();
        }
        catch
        {
            throw new InvalidOperationException(
                $"Nie można utworzyć instancji '{nameof(IdentityUser)}'. Upewnij się, że typ nie jest abstrakcyjny i ma bezparametrowy konstruktor.");
        }
    }

    private IUserEmailStore<IdentityUser> GetEmailStore()
    {
        if (!_userManager.SupportsUserEmail)
            throw new NotSupportedException("Domyślny interfejs użytkownika wymaga magazynu użytkowników obsługującego e-mail.");

        return (IUserEmailStore<IdentityUser>)_userStore;
    }
}
