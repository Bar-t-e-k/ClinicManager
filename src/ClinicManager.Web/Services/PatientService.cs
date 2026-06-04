using ClinicManager.Web.Data;
using ClinicManager.Web.DTOs;
using ClinicManager.Web.Mappers;
using ClinicManager.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace ClinicManager.Web.Services;

public class PatientService : IPatientService
{
    private readonly ClinicDbContext _context;
    private readonly IPatientMapper _mapper;
    private readonly IFileStorageService _fileStorageService;

    public PatientService(ClinicDbContext context, IPatientMapper mapper, IFileStorageService fileStorageService)
    {
        _context = context;
        _mapper = mapper;
        _fileStorageService = fileStorageService;
    }

    public async Task<IEnumerable<PatientDto>> GetAllPatientsAsync(string? searchTerm = null)
    {
        var query = _context.Set<Patient>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p =>
                p.FirstName.Contains(searchTerm) ||
                p.LastName.Contains(searchTerm) ||
                p.Pesel.Contains(searchTerm));
        }

        return await _mapper.QueryablePatientToPatientDto(query).ToListAsync();
    }

    public async Task<PatientDto?> GetPatientByIdAsync(int id)
    {
        var patient = await _context.Set<Patient>()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient == null) return null;

        return _mapper.PatientToPatientDto(patient);
    }

    public async Task<(int? PatientId, string? Error)> CreatePatientAsync(CreateUpdatePatientDto dto)
    {
        if (await PeselExistsAsync(dto.Pesel))
            return (null, "Pacjent z tym numerem PESEL już istnieje w systemie.");

        var patient = _mapper.CreatePatientDtoToPatient(dto);

        _context.Set<Patient>().Add(patient);
        await _context.SaveChangesAsync();

        return (patient.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdatePatientAsync(int id, CreateUpdatePatientDto dto)
    {
        var patient = await _context.Set<Patient>()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient == null) return (false, null);

        if (await PeselExistsAsync(dto.Pesel, id))
            return (false, "Pacjent z tym numerem PESEL już istnieje w systemie.");

        _mapper.UpdatePatientFromDto(dto, patient);
        await _context.SaveChangesAsync();

        return (true, null);
    }

    private async Task<bool> PeselExistsAsync(string pesel, int? excludePatientId = null)
    {
        return await _context.Patients.AnyAsync(p => 
            p.Pesel == pesel && (excludePatientId == null || p.Id != excludePatientId));
    }

    public async Task<bool> DeletePatientAsync(int id)
    {
        var patient = await _context.Patients
            .Include(p => p.MedicalRecords)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient == null) return false;

        var filesToDelete = new List<string>();

        var avatarProp = patient.GetType().GetProperty("AvatarUrl");
        if (avatarProp != null)
        {
            var avatarUrl = avatarProp.GetValue(patient) as string;
            if (!string.IsNullOrEmpty(avatarUrl))
            {
                filesToDelete.Add(avatarUrl);
            }
        }

        if (patient.MedicalRecords != null && patient.MedicalRecords.Any())
        {
            var documentUrls = patient.MedicalRecords
                .Where(r => !string.IsNullOrEmpty(r.FilePath))
                .Select(r => r.FilePath!);

            filesToDelete.AddRange(documentUrls);
        }

        patient.IsDeleted = true;
        await _context.SaveChangesAsync();

        foreach (var filePath in filesToDelete)
        {
            _fileStorageService.DeleteFile(filePath);
        }

        return true;
    }

    public async Task<PatientDetailsDto?> GetPatientDetailsAsync(int id)
    {
        var patient = await _context.Patients
            .Include(p => p.MedicalRecords)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (patient == null) return null;

        return _mapper.PatientToPatientDetailsDto(patient);
    }

    public async Task<bool> AddMedicalRecordAsync(int patientId, string fileName, string filePath)
    {
        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null || patient.IsDeleted) return false;

        var record = new MedicalRecord
        {
            PatientId = patientId,
            FileName = fileName,
            FilePath = filePath,
            UploadDate = DateTime.UtcNow
        };

        _context.Set<MedicalRecord>().Add(record);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<(bool Success, string? Error)> LinkOrCreatePatientForUserAsync(string userId, string pesel, string firstName, string lastName)
    {
        // Konto może być powiązane tylko z jednym pacjentem.
        var alreadyLinked = await _context.Patients.AnyAsync(p => p.UserId == userId);
        if (alreadyLinked)
            return (false, "To konto jest już powiązane z pacjentem.");

        var existing = await _context.Patients.FirstOrDefaultAsync(p => p.Pesel == pesel);
        if (existing != null)
        {
            if (!string.IsNullOrEmpty(existing.UserId))
                return (false, "Pacjent o tym numerze PESEL ma już przypisane konto.");

            existing.UserId = userId;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        var patient = new Patient
        {
            FirstName = firstName,
            LastName = lastName,
            Pesel = pesel,
            UserId = userId
        };
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<int?> GetPatientIdByUserIdAsync(string userId)
    {
        return await _context.Patients
            .Where(p => p.UserId == userId)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<string?> DeleteMedicalRecordAsync(int recordId, int patientId)
    {
        var record = await _context.Set<MedicalRecord>()
            .FirstOrDefaultAsync(r => r.Id == recordId && r.PatientId == patientId);

        if (record == null) return null;

        var filePath = record.FilePath;

        _context.Set<MedicalRecord>().Remove(record);
        await _context.SaveChangesAsync();

        return filePath;
    }
}