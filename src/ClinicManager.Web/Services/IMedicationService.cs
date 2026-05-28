using ClinicManager.Web.DTOs;

namespace ClinicManager.Web.Services;

public interface IMedicationService
{
    Task<IEnumerable<CreateUpdateMedicationDto>> GetAllMedicationsAsync();
    Task<CreateUpdateMedicationDto?> GetMedicationByIdAsync(int id);
    Task<int> CreateMedicationAsync(CreateUpdateMedicationDto dto);
    Task<bool> UpdateMedicationAsync(int id, CreateUpdateMedicationDto dto);
    Task<bool> DeactivateMedicationAsync(int id);
}