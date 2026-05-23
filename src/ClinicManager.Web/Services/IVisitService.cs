using ClinicManager.Web.DTOs;
using ClinicManager.Web.Models;

namespace ClinicManager.Web.Services;

public interface IVisitService
{
    Task<IEnumerable<VisitDto>> GetAllVisitsAsync(string? doctorId = null);
    Task<VisitDetailsDto?> GetVisitDetailsAsync(int id);
    Task<(bool Success, string? Error)> CreateVisitAsync(CreateVisitDto dto);
    Task<bool> UpdateVisitStatusAsync(int id, VisitStatus status);
    Task<bool> DeleteVisitAsync(int id);

    // Notatki kliniczne
    Task<bool> AddClinicalNoteAsync(int visitId, CreateClinicalNoteDto dto);
    Task<bool> DeleteClinicalNoteAsync(int noteId);

    // Leki
    Task<(bool Success, string? Error)> AddMedicationAsync(int visitId, AddMedicationToVisitDto dto);
    Task<bool> RemoveMedicationAsync(int visitMedicationId);
}