using ClinicManager.Web.Data;
using ClinicManager.Web.DTOs;
using ClinicManager.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Web.Services;

public class VisitService : IVisitService
{
    private readonly ClinicDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public VisitService(ClinicDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IEnumerable<VisitDto>> GetAllVisitsAsync(string? doctorId = null)
    {
        var query = _context.Visits
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .Where(v => !v.IsDeleted);

        if (!string.IsNullOrEmpty(doctorId))
            query = query.Where(v => v.DoctorId == doctorId);

        var visits = await query.OrderBy(v => v.ScheduledDate).ToListAsync();
        return visits.Select(v => MapToDto(v));
    }

    public async Task<IEnumerable<VisitDto>> GetVisitsByPatientAsync(int patientId)
    {
        var visits = await _context.Visits
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .Where(v => !v.IsDeleted && v.PatientId == patientId)
            .OrderBy(v => v.ScheduledDate)
            .ToListAsync();

        return visits.Select(MapToDto);
    }

    public async Task<VisitDetailsDto?> GetVisitDetailsAsync(int id)
    {
        var visit = await _context.Visits
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .Include(v => v.ClinicalNotes)
            .Include(v => v.VisitMedications).ThenInclude(vm => vm.Medication)
            .Include(v => v.VisitProcedures).ThenInclude(vp => vp.Procedure)
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (visit == null) return null;

        return new VisitDetailsDto
        {
            Id = visit.Id,
            PatientId = visit.PatientId,
            PatientFullName = $"{visit.Patient.FirstName} {visit.Patient.LastName}",
            DoctorId = visit.DoctorId,
            DoctorName = visit.Doctor.Email ?? visit.Doctor.UserName ?? visit.DoctorId,
            ScheduledDate = visit.ScheduledDate,
            Status = GetStatusDisplay(visit.Status),
            Description = visit.Description,
            TotalCost = visit.TotalCost,
            ClinicalNotes = visit.ClinicalNotes
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new ClinicalNoteDto
                {
                    Id = n.Id,
                    Content = n.Content,
                    CreatedAt = n.CreatedAt
                }).ToList(),
            Medications = visit.VisitMedications
                .Select(vm => new VisitMedicationDto
                {
                    Id = vm.Id,
                    MedicationId = vm.MedicationId,
                    MedicationName = vm.Medication.Name,
                    Quantity = vm.Quantity,
                    UnitPrice = vm.UnitPrice,
                    Dosage = vm.Dosage
                }).ToList(),
            Procedures = visit.VisitProcedures
                .Select(vp => new VisitProcedureDto
                {
                    Id = vp.Id,
                    ProcedureId = vp.ProcedureId,
                    ProcedureDescription = vp.Procedure.Description,
                    Quantity = vp.Quantity,
                    UnitCost = vp.UnitCost
                }).ToList()
        };
    }

    public async Task<(bool Success, string? Error)> CreateVisitAsync(CreateVisitDto dto)
    {
        if (dto.ScheduledDate.Date < DateTime.Today)
            return (false, "Nie można umówić wizyty na datę przeszłą.");

        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null || patient.IsDeleted)
            return (false, "Wskazany pacjent nie istnieje.");

        var doctor = await _userManager.FindByIdAsync(dto.DoctorId);
        if (doctor == null)
            return (false, "Wskazany lekarz nie istnieje.");

        var visit = new Visit
        {
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            ScheduledDate = dto.ScheduledDate,
            Description = dto.Description,
            Status = VisitStatus.Zaplanowana
        };

        _context.Visits.Add(visit);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> UpdateVisitStatusAsync(int id, VisitStatus status)
    {
        if (!Enum.IsDefined(typeof(VisitStatus), status)) return false;

        var visit = await _context.Visits.FindAsync(id);
        if (visit == null || visit.IsDeleted) return false;

        visit.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelVisitAsync(int id)
    {
        var visit = await _context.Visits.FindAsync(id);
        if (visit == null || visit.IsDeleted) return false;

        visit.Status = VisitStatus.Anulowana;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string?> GetVisitDoctorIdAsync(int visitId)
    {
        return await _context.Visits
            .AsNoTracking()
            .Where(v => v.Id == visitId && !v.IsDeleted)
            .Select(v => v.DoctorId)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<VisitDto>> GetPlannedVisitsForDateAsync(DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var visits = await _context.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .Where(v => !v.IsDeleted
                        && v.ScheduledDate >= dayStart
                        && v.ScheduledDate < dayEnd
                        && (v.Status == VisitStatus.Zaplanowana || v.Status == VisitStatus.Potwierdzona))
            .OrderBy(v => v.ScheduledDate)
            .ToListAsync();

        return visits.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ActiveVisitApiDto>> GetActiveVisitsAsync()
    {
        var visits = await _context.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .Include(v => v.VisitMedications)
            .Where(v => !v.IsDeleted
                        && (v.Status == VisitStatus.Zaplanowana
                            || v.Status == VisitStatus.Potwierdzona
                            || v.Status == VisitStatus.WTrakcie))
            .OrderBy(v => v.ScheduledDate)
            .ToListAsync();

        return visits.Select(v => new ActiveVisitApiDto
        {
            Id = v.Id,
            PatientId = v.PatientId,
            DoctorId = v.DoctorId,
            ScheduledDate = v.ScheduledDate,
            Status = GetStatusDisplay(v.Status),
            TotalCost = v.TotalCost,
            MedicationCount = v.VisitMedications.Count
        }).ToList(); ;
    }

    public async Task<bool> AddClinicalNoteAsync(int visitId, CreateClinicalNoteDto dto)
    {
        var visit = await _context.Visits.FindAsync(visitId);
        if (visit == null || visit.IsDeleted) return false;

        _context.ClinicalNotes.Add(new ClinicalNote
        {
            VisitId = visitId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteClinicalNoteAsync(int noteId, int visitId)
    {
        var note = await _context.ClinicalNotes
            .FirstOrDefaultAsync(n => n.Id == noteId && n.VisitId == visitId);

        if (note == null) return false;

        _context.ClinicalNotes.Remove(note);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? Error)> AddMedicationAsync(int visitId, AddMedicationToVisitDto dto)
    {
        var visit = await _context.Visits
            .Include(v => v.VisitMedications)
            .Include(v => v.VisitProcedures)
            .FirstOrDefaultAsync(v => v.Id == visitId && !v.IsDeleted);

        if (visit == null) return (false, "Wizyta nie istnieje.");

        var medication = await _context.Medications.FindAsync(dto.MedicationId);
        if (medication == null || !medication.IsActive)
            return (false, "Lek nie istnieje lub jest nieaktywny.");

        var existing = visit.VisitMedications.FirstOrDefault(vm => vm.MedicationId == dto.MedicationId);
        if (existing != null)
        {
            existing.Quantity += dto.Quantity;
            if (!string.IsNullOrWhiteSpace(dto.Dosage))
                existing.Dosage = dto.Dosage;
        }
        else
        {
            visit.VisitMedications.Add(new VisitMedication
            {
                VisitId = visitId,
                MedicationId = dto.MedicationId,
                Quantity = dto.Quantity,
                UnitPrice = medication.Price,
                Dosage = dto.Dosage
            });
        }

        RecalculateTotalCost(visit);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> RemoveMedicationAsync(int visitMedicationId)
    {
        var vm = await _context.VisitMedications
            .Include(x => x.Visit).ThenInclude(v => v.VisitMedications)
            .Include(x => x.Visit).ThenInclude(v => v.VisitProcedures)
            .FirstOrDefaultAsync(x => x.Id == visitMedicationId);

        if (vm == null) return false;

        var visit = vm.Visit;
        _context.VisitMedications.Remove(vm);
        visit.VisitMedications.Remove(vm);

        RecalculateTotalCost(visit);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? Error)> AddProcedureAsync(int visitId, AddProcedureToVisitDto dto)
    {
        var visit = await _context.Visits
            .Include(v => v.VisitMedications)
            .Include(v => v.VisitProcedures)
            .FirstOrDefaultAsync(v => v.Id == visitId && !v.IsDeleted);

        if (visit == null) return (false, "Wizyta nie istnieje.");

        var procedure = await _context.Procedures.FindAsync(dto.ProcedureId);
        if (procedure == null || !procedure.IsActive)
            return (false, "Procedura nie istnieje lub jest nieaktywna.");

        var existing = visit.VisitProcedures.FirstOrDefault(vp => vp.ProcedureId == dto.ProcedureId);
        if (existing != null)
        {
            existing.Quantity += dto.Quantity;
        }
        else
        {
            visit.VisitProcedures.Add(new VisitProcedure
            {
                VisitId = visitId,
                ProcedureId = dto.ProcedureId,
                Quantity = dto.Quantity,
                UnitCost = procedure.Cost
            });
        }

        RecalculateTotalCost(visit);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> RemoveProcedureAsync(int visitProcedureId)
    {
        var vp = await _context.VisitProcedures
            .Include(x => x.Visit).ThenInclude(v => v.VisitMedications)
            .Include(x => x.Visit).ThenInclude(v => v.VisitProcedures)
            .FirstOrDefaultAsync(x => x.Id == visitProcedureId);

        if (vp == null) return false;

        var visit = vp.Visit;
        _context.VisitProcedures.Remove(vp);
        visit.VisitProcedures.Remove(vp);

        RecalculateTotalCost(visit);
        await _context.SaveChangesAsync();
        return true;
    }

    private static void RecalculateTotalCost(Visit visit)
    {
        var medicationsCost = visit.VisitMedications.Sum(vm => vm.UnitPrice * vm.Quantity);
        var proceduresCost = visit.VisitProcedures.Sum(vp => vp.UnitCost * vp.Quantity);
        visit.TotalCost = medicationsCost + proceduresCost;
    }

    private static VisitDto MapToDto(Visit v) => new()
    {
        Id = v.Id,
        PatientId = v.PatientId,
        PatientFullName = $"{v.Patient.FirstName} {v.Patient.LastName}",
        DoctorId = v.DoctorId,
        DoctorName = v.Doctor.Email ?? v.Doctor.UserName ?? v.DoctorId,
        ScheduledDate = v.ScheduledDate,
        Status = GetStatusDisplay(v.Status),
        Description = v.Description,
        TotalCost = v.TotalCost
    };

    public static string GetStatusDisplay(VisitStatus status) => status switch
    {
        VisitStatus.Zaplanowana => "Zaplanowana",
        VisitStatus.Potwierdzona => "Potwierdzona",
        VisitStatus.WTrakcie => "W trakcie",
        VisitStatus.Zakonczona => "Zakończona",
        VisitStatus.Anulowana => "Anulowana",
        _ => status.ToString()
    };
}