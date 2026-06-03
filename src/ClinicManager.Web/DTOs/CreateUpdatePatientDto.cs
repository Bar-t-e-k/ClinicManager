using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Web.DTOs;

public class CreateUpdatePatientDto
{
    [Required(ErrorMessage = "Imię jest wymagane.")]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nazwisko jest wymagane.")]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "PESEL jest wymagany.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "PESEL musi składać się z dokładnie 11 cyfr.")]
    public string Pesel { get; set; } = string.Empty;

    public string? InsuranceNumber { get; set; }

    public IFormFile? AvatarFile { get; set; }

    public string? AvatarUrl { get; set; }
}