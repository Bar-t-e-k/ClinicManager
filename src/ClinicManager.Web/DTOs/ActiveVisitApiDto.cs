namespace ClinicManager.Web.DTOs;

/// <summary>
/// Zanonimizowane DTO dla endpointu wydajnościowego GET /api/visits/active.
/// Zabezpiecza wrażliwe dane przed wyciekiem na publicznym endpoincie.
/// </summary>
public class ActiveVisitApiDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string DoctorId { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public int MedicationCount { get; set; }
}
