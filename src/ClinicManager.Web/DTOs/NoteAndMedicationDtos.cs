using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Web.DTOs;

public class CreateClinicalNoteDto
{
    [Required(ErrorMessage = "Treść notatki jest wymagana.")]
    [StringLength(2000, MinimumLength = 3, ErrorMessage = "Notatka musi mieć od 3 do 2000 znaków.")]
    [Display(Name = "Treść notatki klinicznej")]
    public string Content { get; set; } = string.Empty;
}

public class AddMedicationToVisitDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Wybierz lek.")]
    public int MedicationId { get; set; }

    [Range(1, 100, ErrorMessage = "Ilość musi być między 1 a 100.")]
    [Display(Name = "Ilość")]
    public int Quantity { get; set; } = 1;

    [StringLength(300, ErrorMessage = "Dawkowanie może mieć maksymalnie 300 znaków.")]
    [Display(Name = "Dawkowanie")]
    public string? Dosage { get; set; }
}

public class AddProcedureToVisitDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Wybierz procedurę.")]
    public int ProcedureId { get; set; }

    [Range(1, 100, ErrorMessage = "Ilość musi być między 1 a 100.")]
    [Display(Name = "Ilość")]
    public int Quantity { get; set; } = 1;
}