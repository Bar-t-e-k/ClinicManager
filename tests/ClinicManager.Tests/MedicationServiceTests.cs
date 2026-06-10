using ClinicManager.Web.Data;
using ClinicManager.Web.DTOs;
using ClinicManager.Web.Models;
using ClinicManager.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Tests;

public class MedicationServiceTests
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
    public async Task CreateMedicationAsync_ShouldAddMedicationToDatabase()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var service = new MedicationService(context);
        var dto = new CreateUpdateMedicationDto { Name = "Aspirin", Price = 10m, IsActive = true };

        // Act
        var resultId = await service.CreateMedicationAsync(dto);

        // Assert
        var medicationInDb = await context.Medications.FindAsync(resultId);
        Assert.NotNull(medicationInDb);
        Assert.Equal("Aspirin", medicationInDb.Name);
    }

    [Fact]
    public async Task GetAllMedicationsAsync_ShouldReturnAllMedications()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        context.Medications.Add(new Medication { Name = "Lek A", IsActive = true });
        context.Medications.Add(new Medication { Name = "Lek B", IsActive = true });
        await context.SaveChangesAsync();
        var service = new MedicationService(context);

        // Act
        var result = await service.GetAllMedicationsAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task UpdateMedicationAsync_ShouldUpdateAndReturnTrue()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var med = new Medication { Name = "Stara Nazwa", IsActive = true };
        context.Medications.Add(med);
        await context.SaveChangesAsync();

        var service = new MedicationService(context);
        var dto = new CreateUpdateMedicationDto { Name = "Nowa Nazwa", IsActive = false };

        // Act
        var success = await service.UpdateMedicationAsync(med.Id, dto);

        // Assert
        Assert.True(success);
        var updated = await context.Medications.FindAsync(med.Id);
        Assert.Equal("Nowa Nazwa", updated.Name);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task DeactivateMedicationAsync_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var med = new Medication { Name = "Lek", IsActive = true };
        context.Medications.Add(med);
        await context.SaveChangesAsync();

        var service = new MedicationService(context);

        // Act
        var success = await service.DeactivateMedicationAsync(med.Id);

        // Assert
        Assert.True(success);
        var updated = await context.Medications.FindAsync(med.Id);
        Assert.False(updated.IsActive);
    }
}