using ClinicManager.Web.Data;
using ClinicManager.Web.DTOs;
using ClinicManager.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Web.Services;

public class MedicationService : IMedicationService
{
    private readonly ClinicDbContext _context;

    public MedicationService(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MedicationDto>> GetAllMedicationsAsync()
    {
        return await _context.Medications
            .OrderBy(m => m.Name)
            .Select(m => new MedicationDto
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                Price = m.Price,
                IsActive = m.IsActive
            })
            .ToListAsync();
    }

    public async Task<MedicationDto?> GetMedicationByIdAsync(int id)
    {
        var m = await _context.Medications.FindAsync(id);
        if (m == null) return null;

        return new MedicationDto { Id = m.Id, Name = m.Name, Description = m.Description, Price = m.Price, IsActive = m.IsActive };
    }

    public async Task<int> CreateMedicationAsync(CreateMedicationDto dto)
    {
        var medication = new Medication
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            IsActive = true
        };
        _context.Medications.Add(medication);
        await _context.SaveChangesAsync();
        return medication.Id;
    }

    public async Task<bool> UpdateMedicationAsync(int id, CreateMedicationDto dto)
    {
        var medication = await _context.Medications.FindAsync(id);
        if (medication == null) return false;

        medication.Name = dto.Name;
        medication.Description = dto.Description;
        medication.Price = dto.Price;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateMedicationAsync(int id)
    {
        var medication = await _context.Medications.FindAsync(id);
        if (medication == null) return false;

        medication.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }
}