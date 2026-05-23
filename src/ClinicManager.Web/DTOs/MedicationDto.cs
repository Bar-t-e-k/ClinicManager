using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Web.DTOs;

public class MedicationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

public class CreateMedicationDto
{
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
}