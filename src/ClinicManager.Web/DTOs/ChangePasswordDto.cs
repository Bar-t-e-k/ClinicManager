using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Web.DTOs;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Obecne hasło jest wymagane.")]
    [DataType(DataType.Password)]
    [Display(Name = "Obecne hasło")]
    public string CurrentPassword { get; set; } = null!;

    [Required(ErrorMessage = "Nowe hasło jest wymagane.")]
    [StringLength(100, ErrorMessage = "{0} musi mieć co najmniej {2} i maksymalnie {1} znaków.", MinimumLength = 4)]
    [DataType(DataType.Password)]
    [Display(Name = "Nowe hasło")]
    public string NewPassword { get; set; } = null!;

    [DataType(DataType.Password)]
    [Display(Name = "Potwierdź nowe hasło")]
    [Compare("NewPassword", ErrorMessage = "Nowe hasło i hasło potwierdzające nie są zgodne.")]
    public string ConfirmPassword { get; set; } = null!;
}