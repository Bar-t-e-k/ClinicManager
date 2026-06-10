using ClinicManager.Web.DTOs;

namespace ClinicManager.Web.Services;

public interface IPatientService
{
    Task<IEnumerable<PatientDto>> GetAllPatientsAsync(string? searchTerm = null);

    Task<PatientDto?> GetPatientByIdAsync(int id);

    Task<(int? PatientId, string? Error)> CreatePatientAsync(CreateUpdatePatientDto dto);

    Task<(bool Success, string? Error)> UpdatePatientAsync(int id, CreateUpdatePatientDto dto);

    Task<bool> DeletePatientAsync(int id);

    Task<PatientDetailsDto?> GetPatientDetailsAsync(int id);

    Task<bool> AddMedicalRecordAsync(int patientId, string fileName, string filePath);

    Task<string?> DeleteMedicalRecordAsync(int recordId, int patientId);

    /// <summary>Powiązuje konto użytkownika z istniejącym pacjentem o danym PESEL lub tworzy nowy rekord pacjenta.</summary>
    Task<(bool Success, string? Error)> LinkOrCreatePatientForUserAsync(string userId, string pesel, string firstName, string lastName);

    /// <summary>Zwraca Id pacjenta powiązanego z danym kontem użytkownika (lub null, jeśli brak powiązania).</summary>
    Task<int?> GetPatientIdByUserIdAsync(string userId);

    Task<bool> ReactivatePatientAsync(int id);

    Task<IEnumerable<PatientDto>> GetDeletedPatientsAsync(string? searchTerm = null);
}