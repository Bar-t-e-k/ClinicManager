using ClinicManager.Web.Data;
using ClinicManager.Web.DTOs;
using ClinicManager.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Web.Services;

public class ProcedureService : IProcedureService
{
    private readonly ClinicDbContext _context;

    public ProcedureService(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CreateUpdateProcedureDto>> GetAllProceduresAsync()
    {
        return await _context.Procedures
            .OrderBy(p => p.Description)
            .Select(p => new CreateUpdateProcedureDto
            {
                Id = p.Id,
                Description = p.Description,
                Cost = p.Cost,
                IsActive = p.IsActive
            })
            .ToListAsync();
    }

    public async Task<CreateUpdateProcedureDto?> GetProcedureByIdAsync(int id)
    {
        var p = await _context.Procedures.FindAsync(id);
        if (p == null) return null;

        return new CreateUpdateProcedureDto { Id = p.Id, Description = p.Description, Cost = p.Cost, IsActive = p.IsActive };
    }

    public async Task<int> CreateProcedureAsync(CreateUpdateProcedureDto dto)
    {
        var procedure = new Procedure
        {
            Description = dto.Description,
            Cost = dto.Cost,
            IsActive = dto.IsActive
        };
        _context.Procedures.Add(procedure);
        await _context.SaveChangesAsync();
        return procedure.Id;
    }

    public async Task<bool> UpdateProcedureAsync(int id, CreateUpdateProcedureDto dto)
    {
        var procedure = await _context.Procedures.FindAsync(id);
        if (procedure == null) return false;

        procedure.Description = dto.Description;
        procedure.Cost = dto.Cost;
        procedure.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateProcedureAsync(int id)
    {
        var procedure = await _context.Procedures.FindAsync(id);
        if (procedure == null) return false;

        procedure.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }
}