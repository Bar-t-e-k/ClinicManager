using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Web.DTOs;

public class CreateVisitDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Pacjent jest wymagany.")]
    [Display(Name = "Pacjent")]
    public int PatientId { get; set; }

    [Required(ErrorMessage = "Lekarz jest wymagany.")]
    [Display(Name = "Lekarz")]
    public string DoctorId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data wizyty jest wymagana.")]
    [Display(Name = "Data i godzina wizyty")]
    public DateTime ScheduledDate { get; set; } = DateTime.Today.AddDays(1).AddHours(8);

    [StringLength(500)]
    [Display(Name = "Opis / cel wizyty")]
    public string? Description { get; set; }
}