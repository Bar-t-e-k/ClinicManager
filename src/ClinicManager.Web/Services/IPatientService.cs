using ClinicManager.Web.DTOs;

namespace ClinicManager.Web.Services;

public interface IPatientService
{
    Task<IEnumerable<PatientDto>> GetAllPatientsAsync(string? searchTerm = null);

    Task<PatientDto?> GetPatientByIdAsync(int id);

    Task<int> CreatePatientAsync(CreateUpdatePatientDto dto);

    Task<bool> UpdatePatientAsync(int id, CreateUpdatePatientDto dto);

    Task<bool> DeletePatientAsync(int id);

    Task<PatientDetailsDto?> GetPatientDetailsAsync(int id);

    Task<bool> AddMedicalRecordAsync(int patientId, string fileName, string filePath);

    Task<string?> DeleteMedicalRecordAsync(int recordId, int patientId);
}