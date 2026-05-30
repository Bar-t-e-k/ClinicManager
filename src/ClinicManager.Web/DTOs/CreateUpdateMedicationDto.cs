using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Web.DTOs;

public class CreateUpdateMedicationDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nazwa leku jest wymagana.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Nazwa musi mieć od 2 do 200 znaków.")]
    [Display(Name = "Nazwa leku")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Opis / wskazania")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Cena jest wymagana.")]
    [Range(0.01, 100000, ErrorMessage = "Cena musi być większa od 0.")]
    [Display(Name = "Cena (PLN)")]
    public decimal Price { get; set; }

    [Display(Name = "Czy lek jest aktywny?")]
    public bool IsActive { get; set; } = true;
}