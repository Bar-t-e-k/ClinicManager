using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ClinicManager.Web.Models;

public class Visit
{
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    [Required]
    public string DoctorId { get; set; } = string.Empty;
    public IdentityUser Doctor { get; set; } = null!;

    [Required]
    public DateTime ScheduledDate { get; set; }

    public VisitStatus Status { get; set; } = VisitStatus.Zaplanowana;

    [StringLength(500)]
    public string? Description { get; set; }

    public decimal TotalCost { get; set; }

    public bool IsDeleted { get; set; } = false;

    public ICollection<ClinicalNote> ClinicalNotes { get; set; } = new List<ClinicalNote>();
    public ICollection<VisitMedication> VisitMedications { get; set; } = new List<VisitMedication>();
}