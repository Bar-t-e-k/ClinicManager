namespace ClinicManager.Web.DTOs;

public class VisitDetailsDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientFullName { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TotalCost { get; set; }
    public List<ClinicalNoteDto> ClinicalNotes { get; set; } = new();
    public List<VisitMedicationDto> Medications { get; set; } = new();
    public List<VisitProcedureDto> Procedures { get; set; } = new();
}

public class ClinicalNoteDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class VisitMedicationDto
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Dosage { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}

public class VisitProcedureDto
{
    public int Id { get; set; }
    public int ProcedureId { get; set; }
    public string ProcedureDescription { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost => Quantity * UnitCost;
}