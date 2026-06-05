using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Web.Models;

public class Procedure
{
    public int Id { get; set; }

    [Required]
    [StringLength(300, MinimumLength = 2, ErrorMessage = "Opis musi mieć od 2 do 300 znaków.")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 1000000, ErrorMessage = "Koszt świadczenia musi być większy od 0.")]
    public decimal Cost { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<VisitProcedure> VisitProcedures { get; set; } = new List<VisitProcedure>();
}
