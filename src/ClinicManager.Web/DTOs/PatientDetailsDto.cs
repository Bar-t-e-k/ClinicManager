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

    public List<MedicalRecordDto> MedicalRecords { get; set; } = [];

    public List<VisitDto> Visits { get; set; } = [];

    public string? AvatarUrl { get; set; }
}