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
    private readonly PatientMapper _mapper;

    public PatientService(ClinicDbContext context)
    {
        _context = context;
        _mapper = new PatientMapper();
    }

    public async Task<IEnumerable<PatientDto>> GetAllPatientsAsync(string? searchTerm = null)
    {
        var query = _context.Set<Patient>().Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => p.LastName.Contains(searchTerm) || p.Pesel.Contains(searchTerm));
        }

        var patients = await query.ToListAsync();
        return patients.Select(p => _mapper.PatientToPatientDto(p));
    }

    public async Task<PatientDto?> GetPatientByIdAsync(int id)
    {
        var patient = await _context.Set<Patient>()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (patient == null) return null;

        return _mapper.PatientToPatientDto(patient);
    }

    public async Task<int> CreatePatientAsync(CreateUpdatePatientDto dto)
    {
        var patient = _mapper.CreatePatientDtoToPatient(dto);

        _context.Set<Patient>().Add(patient);
        await _context.SaveChangesAsync();

        return patient.Id;
    }

    public async Task<bool> UpdatePatientAsync(int id, CreateUpdatePatientDto dto)
    {
        var patient = await _context.Set<Patient>()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (patient == null) return false;

        _mapper.UpdatePatientFromDto(dto, patient);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeletePatientAsync(int id)
    {
        var patient = await _context.Set<Patient>().FindAsync(id);

        if (patient == null || patient.IsDeleted) return false;

        patient.IsDeleted = true;

        await _context.SaveChangesAsync();
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

    public async Task<string?> DeleteMedicalRecordAsync(int recordId)
    {
        var record = await _context.Set<MedicalRecord>().FindAsync(recordId);

        if (record == null) return null;

        var filePath = record.FilePath;

        _context.Set<MedicalRecord>().Remove(record);
        await _context.SaveChangesAsync();

        return filePath;
    }
}