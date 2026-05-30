using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Web.Models;

public class ClinicalNote
{
    public int Id { get; set; }

    [Required]
    public int VisitId { get; set; }
    public Visit Visit { get; set; } = null!;

    [Required]
    [StringLength(2000, MinimumLength = 3, ErrorMessage = "Notatka musi mieć od 3 do 2000 znaków.")]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}