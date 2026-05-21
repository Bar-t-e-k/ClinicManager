using ClinicManager.Web.Data;
using ClinicManager.Web.DTOs;
using ClinicManager.Web.Models;
using ClinicManager.Web.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicManager.Tests;

public class PatientServiceTests
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
    public async Task CreatePatientAsync_ShouldAddPatientToDatabase()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var service = new PatientService(context);

        var dto = new CreateUpdatePatientDto
        {
            FirstName = "Jan",
            LastName = "Kowalski",
            Pesel = "12345678901"
        };

        // Act
        var resultId = await service.CreatePatientAsync(dto);

        // Assert
        var patientInDb = await context.Set<Patient>().FindAsync(resultId);

        Assert.NotNull(patientInDb); 
        Assert.Equal("Jan", patientInDb.FirstName);
        Assert.False(patientInDb.IsDeleted);
    }

    [Fact]
    public async Task DeletePatientAsync_ShouldSetIsDeletedFlagToTrue_InsteadOfHardDelete()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var patient = new Patient { FirstName = "Anna", LastName = "Nowak", Pesel = "11122233344" };
        context.Set<Patient>().Add(patient);
        await context.SaveChangesAsync();

        var service = new PatientService(context);

        // Act
        var result = await service.DeletePatientAsync(patient.Id);

        // Assert
        Assert.True(result);

        var patientInDb = await context.Set<Patient>().FindAsync(patient.Id);
        Assert.NotNull(patientInDb); 
        Assert.True(patientInDb.IsDeleted);
    }

    [Fact]
    public async Task GetAllPatientsAsync_ShouldReturnOnlyActivePatients()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        context.Set<Patient>().AddRange(
            new Patient { FirstName = "Aktywny", LastName = "Pacjent", Pesel = "11111111111", IsDeleted = false },
            new Patient { FirstName = "Usunięty", LastName = "Pacjent", Pesel = "22222222222", IsDeleted = true }
        );
        await context.SaveChangesAsync();

        var service = new PatientService(context);

        // Act
        var result = await service.GetAllPatientsAsync(null);

        // Assert
        Assert.Single(result);
        Assert.Equal("Aktywny", result.First().FirstName);
    }
}