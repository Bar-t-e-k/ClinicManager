namespace ClinicManager.Web.DTOs;

/// <summary>
/// DTO dla endpointu wydajnościowego GET /api/visits/active (JOIN pacjent + lekarz + leki).
/// </summary>
public class ActiveVisitApiDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientFullName { get; set; } = string.Empty;
    public string PatientPesel { get; set; } = string.Empty;
    public string? PatientInsuranceNumber { get; set; }
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TotalCost { get; set; }
    public int MedicationCount { get; set; }
}
