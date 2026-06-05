using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Web.Models;

public class VisitMedication
{
    public int Id { get; set; }

    public int VisitId { get; set; }
    public Visit Visit { get; set; } = null!;

    public int MedicationId { get; set; }
    public Medication Medication { get; set; } = null!;

    public int Quantity { get; set; } = 1;

    /// <summary>Snapshot ceny w momencie przepisania leku.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Zalecane dawkowanie leku (np. "1 tabletka 2x dziennie po posiłku").</summary>
    [StringLength(300)]
    public string? Dosage { get; set; }
}