using ClinicManager.Web.Data;
using ClinicManager.Web.DTOs;
using ClinicManager.Web.Models;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
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

public class VisitServiceTests
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

    private UserManager<IdentityUser> GetMockUserManager(IdentityUser? doctorToReturn = null)
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        var userManager = new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManager
            .Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(doctorToReturn);

        return userManager.Object;
    }

    [Fact]
    public async Task CreateVisitAsync_ShouldFail_WhenDateIsInThePast()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var userManager = GetMockUserManager(new IdentityUser { Id = "doc1", Email = "lekarz@clinic.com" });
        var service = new VisitService(context, userManager);

        var patient = new Patient { FirstName = "Jan", LastName = "Kowalski", Pesel = "12345678901" };
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var dto = new CreateVisitDto
        {
            PatientId = patient.Id,
            DoctorId = "doc1",
            ScheduledDate = DateTime.Today.AddDays(-1)
        };

        // Act
        var (success, error) = await service.CreateVisitAsync(dto);

        // Assert
        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task CreateVisitAsync_ShouldSucceed_WhenDataIsValid()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var userManager = GetMockUserManager(new IdentityUser { Id = "doc1", Email = "lekarz@clinic.com" });
        var service = new VisitService(context, userManager);

        var patient = new Patient { FirstName = "Jan", LastName = "Kowalski", Pesel = "12345678901" };
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var dto = new CreateVisitDto
        {
            PatientId = patient.Id,
            DoctorId = "doc1",
            ScheduledDate = DateTime.Today.AddDays(1)
        };

        // Act
        var (success, error) = await service.CreateVisitAsync(dto);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(1, await context.Visits.CountAsync());
    }

    [Fact]
    public async Task DeleteVisitAsync_ShouldSetIsDeletedFlag()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var userManager = GetMockUserManager();
        var service = new VisitService(context, userManager);

        var visit = new Visit
        {
            PatientId = 1,
            DoctorId = "doc1",
            ScheduledDate = DateTime.Today.AddDays(1),
            Status = VisitStatus.Zaplanowana
        };
        context.Visits.Add(visit);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteVisitAsync(visit.Id);

        // Assert
        Assert.True(result);
        var visitInDb = await context.Visits.FindAsync(visit.Id);
        Assert.True(visitInDb!.IsDeleted);
    }

    [Fact]
    public async Task AddClinicalNoteAsync_ShouldAddNoteToVisit()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var userManager = GetMockUserManager();
        var service = new VisitService(context, userManager);

        var visit = new Visit
        {
            PatientId = 1,
            DoctorId = "doc1",
            ScheduledDate = DateTime.Today.AddDays(1),
            Status = VisitStatus.Zaplanowana
        };
        context.Visits.Add(visit);
        await context.SaveChangesAsync();

        var dto = new CreateClinicalNoteDto { Content = "Pacjent skarży się na ból głowy." };

        // Act
        var result = await service.AddClinicalNoteAsync(visit.Id, dto);

        // Assert
        Assert.True(result);
        Assert.Equal(1, await context.ClinicalNotes.CountAsync());
    }

    [Fact]
    public async Task AddMedicationAsync_ShouldIncreaseTotalCost()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var userManager = GetMockUserManager();
        var service = new VisitService(context, userManager);

        var visit = new Visit
        {
            PatientId = 1,
            DoctorId = "doc1",
            ScheduledDate = DateTime.Today.AddDays(1),
            Status = VisitStatus.Zaplanowana
        };
        context.Visits.Add(visit);

        var medication = new Medication { Name = "Apap", Price = 15.00m, IsActive = true };
        context.Medications.Add(medication);
        await context.SaveChangesAsync();

        var dto = new AddMedicationToVisitDto { MedicationId = medication.Id, Quantity = 2 };

        // Act
        var (success, error) = await service.AddMedicationAsync(visit.Id, dto);

        // Assert
        Assert.True(success);
        var visitInDb = await context.Visits.FindAsync(visit.Id);
        Assert.Equal(30.00m, visitInDb!.TotalCost);
    }

    [Fact]
    public async Task UpdateVisitStatusAsync_ShouldChangeStatus()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var userManager = GetMockUserManager();
        var service = new VisitService(context, userManager);

        var visit = new Visit
        {
            PatientId = 1,
            DoctorId = "doc1",
            ScheduledDate = DateTime.Today.AddDays(1),
            Status = VisitStatus.Zaplanowana
        };
        context.Visits.Add(visit);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateVisitStatusAsync(visit.Id, VisitStatus.Potwierdzona);

        // Assert
        Assert.True(result);
        var visitInDb = await context.Visits.FindAsync(visit.Id);
        Assert.Equal(VisitStatus.Potwierdzona, visitInDb!.Status);
    }
}