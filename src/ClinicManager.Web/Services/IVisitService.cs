using ClinicManager.Web.DTOs;
using ClinicManager.Web.Models;

namespace ClinicManager.Web.Services;

public interface IVisitService
{
    Task<IEnumerable<VisitDto>> GetAllVisitsAsync(string? doctorId = null);
    Task<VisitDetailsDto?> GetVisitDetailsAsync(int id);
    Task<(bool Success, string? Error)> CreateVisitAsync(CreateVisitDto dto);
    Task<bool> UpdateVisitStatusAsync(int id, VisitStatus status);
    Task<bool> CancelVisitAsync(int id);
    Task<string?> GetVisitDoctorIdAsync(int visitId);
    Task<IReadOnlyList<VisitDto>> GetPlannedVisitsForDateAsync(DateTime date);
    Task<IReadOnlyList<ActiveVisitApiDto>> GetActiveVisitsAsync();
    Task<bool> AddClinicalNoteAsync(int visitId, CreateClinicalNoteDto dto);
    Task<bool> DeleteClinicalNoteAsync(int noteId, int visitId);
    Task<(bool Success, string? Error)> AddMedicationAsync(int visitId, AddMedicationToVisitDto dto);
    Task<bool> RemoveMedicationAsync(int visitMedicationId);
}