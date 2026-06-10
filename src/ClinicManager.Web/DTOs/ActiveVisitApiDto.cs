namespace ClinicManager.Web.DTOs;

/// <summary>
/// Zanonimizowane DTO dla endpointu wydajnościowego GET /api/visits/active.
/// Zapytanie w bazie nadal łączy wizyty z pacjentem i lekarzem (JOIN), ale odpowiedź API
/// zawiera wyłącznie identyfikatory – bez danych osobowych (PII).
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
