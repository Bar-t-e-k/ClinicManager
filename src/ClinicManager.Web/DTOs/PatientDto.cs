namespace ClinicManager.Web.DTOs;

public class PatientDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Pesel { get; set; } = string.Empty;
    public string? InsuranceNumber { get; set; }

    public string? AvatarUrl { get; set; }
}