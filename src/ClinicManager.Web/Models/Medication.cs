using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Web.Models;

public class Medication
{
    public int Id { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Nazwa musi mieć od 2 do 200 znaków.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Range(0.01, 100000, ErrorMessage = "Cena musi być większa od 0.")]
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<VisitMedication> VisitMedications { get; set; } = new List<VisitMedication>();
}