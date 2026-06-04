using ClinicManager.Web.DTOs;

namespace ClinicManager.Web.Services;

public interface IProcedureService
{
    Task<IEnumerable<CreateUpdateProcedureDto>> GetAllProceduresAsync();
    Task<CreateUpdateProcedureDto?> GetProcedureByIdAsync(int id);
    Task<int> CreateProcedureAsync(CreateUpdateProcedureDto dto);
    Task<bool> UpdateProcedureAsync(int id, CreateUpdateProcedureDto dto);
    Task<bool> DeactivateProcedureAsync(int id);
}
