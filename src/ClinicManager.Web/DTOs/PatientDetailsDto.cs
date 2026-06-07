using System.Collections.Generic;

namespace ClinicManager.Web.DTOs;

public class PatientDetailsDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Pesel { get; set; } = string.Empty;
    public string? InsuranceNumber { get; set; }

    public string? UserId { get; set; }

    public List<MedicalRecordDto> MedicalRecords { get; set; } = new List<MedicalRecordDto>();

    public List<VisitDto> Visits { get; set; } = new List<VisitDto>();

    public string? AvatarUrl { get; set; }
}