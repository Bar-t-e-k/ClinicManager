using ClinicManager.Web.Data;
using ClinicManager.Web.DTOs;
using ClinicManager.Web.Models;
using ClinicManager.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Tests;

public class ProcedureServiceTests
{
    private async Task<ClinicDbContext> GetInMemoryDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ClinicDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    [Fact]
    public async Task CreateProcedureAsync_ShouldAddProcedureToDatabase()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var service = new ProcedureService(context);
        var dto = new CreateUpdateProcedureDto { Description = "USG", Cost = 150m, IsActive = true };

        // Act
        var resultId = await service.CreateProcedureAsync(dto);

        // Assert
        var procedureInDb = await context.Procedures.FindAsync(resultId);
        Assert.NotNull(procedureInDb);
        Assert.Equal("USG", procedureInDb.Description);
    }

    [Fact]
    public async Task GetAllProceduresAsync_ShouldReturnAllProcedures()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        context.Procedures.Add(new Procedure { Description = "Proc A", IsActive = true });
        context.Procedures.Add(new Procedure { Description = "Proc B", IsActive = true });
        await context.SaveChangesAsync();
        var service = new ProcedureService(context);

        // Act
        var result = await service.GetAllProceduresAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task UpdateProcedureAsync_ShouldUpdateAndReturnTrue()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var proc = new Procedure { Description = "Stara", Cost = 50, IsActive = true };
        context.Procedures.Add(proc);
        await context.SaveChangesAsync();

        var service = new ProcedureService(context);
        var dto = new CreateUpdateProcedureDto { Description = "Nowa", Cost = 100, IsActive = false };

        // Act
        var success = await service.UpdateProcedureAsync(proc.Id, dto);

        // Assert
        Assert.True(success);
        var updated = await context.Procedures.FindAsync(proc.Id);
        Assert.Equal("Nowa", updated.Description);
        Assert.Equal(100, updated.Cost);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task DeactivateProcedureAsync_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var proc = new Procedure { Description = "Proc", IsActive = true };
        context.Procedures.Add(proc);
        await context.SaveChangesAsync();

        var service = new ProcedureService(context);

        // Act
        var success = await service.DeactivateProcedureAsync(proc.Id);

        // Assert
        Assert.True(success);
        var updated = await context.Procedures.FindAsync(proc.Id);
        Assert.False(updated.IsActive);
    }
}