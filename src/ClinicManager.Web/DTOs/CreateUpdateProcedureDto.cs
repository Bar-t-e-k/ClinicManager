using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Web.DTOs;

public class CreateUpdateProcedureDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Opis procedury jest wymagany.")]
    [StringLength(300, MinimumLength = 2, ErrorMessage = "Opis musi mieć od 2 do 300 znaków.")]
    [Display(Name = "Opis procedury")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Koszt świadczenia jest wymagany.")]
    [Range(0.01, 1000000, ErrorMessage = "Koszt świadczenia musi być większy od 0.")]
    [Display(Name = "Koszt świadczenia (PLN)")]
    public decimal Cost { get; set; }

    [Display(Name = "Czy procedura jest aktywna?")]
    public bool IsActive { get; set; } = true;
}