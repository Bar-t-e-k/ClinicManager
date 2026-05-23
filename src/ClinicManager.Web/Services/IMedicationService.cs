using ClinicManager.Web.DTOs;

namespace ClinicManager.Web.Services;

public interface IMedicationService
{
    Task<IEnumerable<MedicationDto>> GetAllMedicationsAsync();
    Task<MedicationDto?> GetMedicationByIdAsync(int id);
    Task<int> CreateMedicationAsync(CreateMedicationDto dto);
    Task<bool> UpdateMedicationAsync(int id, CreateMedicationDto dto);
    Task<bool> DeactivateMedicationAsync(int id);
}