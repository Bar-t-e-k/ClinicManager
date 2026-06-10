using ClinicManager.Web.Data;
using ClinicManager.Web.DTOs;
using ClinicManager.Web.Mappers;
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
        var fileStorageService = new Mock<IFileStorageService>();
        var userStoreMock = new Mock<IUserStore<IdentityUser>>();
        var userManagerMock = new Mock<UserManager<IdentityUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var service = new PatientService(context, new PatientMapper(), fileStorageService.Object, userManagerMock.Object);

        var dto = new CreateUpdatePatientDto
        {
            FirstName = "Jan",
            LastName = "Kowalski",
            Pesel = "12345678901"
        };

        // Act
        var (resultId, error) = await service.CreatePatientAsync(dto);

        // Assert
        Assert.Null(error);
        Assert.NotNull(resultId);
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

        var fileStorageService = new Mock<IFileStorageService>();
        var userStoreMock = new Mock<IUserStore<IdentityUser>>();
        var userManagerMock = new Mock<UserManager<IdentityUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var service = new PatientService(context, new PatientMapper(), fileStorageService.Object, userManagerMock.Object);

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

        var fileStorageService = new Mock<IFileStorageService>();
        var userStoreMock = new Mock<IUserStore<IdentityUser>>();
        var userManagerMock = new Mock<UserManager<IdentityUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var service = new PatientService(context, new PatientMapper(), fileStorageService.Object, userManagerMock.Object);

        // Act
        var result = await service.GetAllPatientsAsync(null);

        // Assert
        Assert.Single(result);
        Assert.Equal("Aktywny", result.First().FirstName);
    }
    [Fact]
    public async Task AddMedicalRecordAsync_ShouldAddRecordToDatabase()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var patient = new Patient { FirstName = "Jan", LastName = "Plikowy", Pesel = "00000000000" };
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var fileStorageService = new Mock<IFileStorageService>();
        var userStoreMock = new Mock<IUserStore<IdentityUser>>();
        var userManagerMock = new Mock<UserManager<IdentityUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var service = new PatientService(context, new PatientMapper(), fileStorageService.Object, userManagerMock.Object);

        // Act
        var result = await service.AddMedicalRecordAsync(patient.Id, "wyniki-krwi.pdf", "/uploads/wyniki-krwi.pdf");

        // Assert
        Assert.True(result);
        var recordsInDb = await context.Set<MedicalRecord>().ToListAsync();
        Assert.Single(recordsInDb);
        Assert.Equal("wyniki-krwi.pdf", recordsInDb.First().FileName);
        Assert.Equal(patient.Id, recordsInDb.First().PatientId);
    }

    [Fact]
    public async Task DeleteMedicalRecordAsync_ShouldRemoveRecordAndReturnPath()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var patient = new Patient { FirstName = "Anna", LastName = "Usuwana", Pesel = "11111111111" };
        var record = new MedicalRecord { FileName = "do-usuniecia.jpg", FilePath = "/uploads/do-usuniecia.jpg" };
        patient.MedicalRecords.Add(record);
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var fileStorageService = new Mock<IFileStorageService>();
        var userStoreMock = new Mock<IUserStore<IdentityUser>>();
        var userManagerMock = new Mock<UserManager<IdentityUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var service = new PatientService(context, new PatientMapper(), fileStorageService.Object, userManagerMock.Object);

        // Act
        var returnedPath = await service.DeleteMedicalRecordAsync(record.Id, patient.Id);

        // Assert
        Assert.Equal("/uploads/do-usuniecia.jpg", returnedPath); 
        var recordsInDb = await context.Set<MedicalRecord>().ToListAsync();
        Assert.Empty(recordsInDb); 
    }

    [Fact]
    public async Task GetPatientByIdAsync_ShouldReturnPatient_WhenExists()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var patient = new Patient { FirstName = "Marek", LastName = "Testowy", Pesel = "12312312312" };
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var fileStorageService = new Mock<IFileStorageService>();
        var userStoreMock = new Mock<IUserStore<IdentityUser>>();
        var userManagerMock = new Mock<UserManager<IdentityUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var service = new PatientService(context, new PatientMapper(), fileStorageService.Object, userManagerMock.Object);

        // Act
        var result = await service.GetPatientByIdAsync(patient.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Marek", result.FirstName);
        Assert.Equal("12312312312", result.Pesel);
    }

    [Fact]
    public async Task UpdatePatientAsync_ShouldUpdatePatient_WhenDataIsValid()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var patient = new Patient { FirstName = "Stary", LastName = "Testowy", Pesel = "12312312312" };
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var fileStorageService = new Mock<IFileStorageService>();
        var userStoreMock = new Mock<IUserStore<IdentityUser>>();
        var userManagerMock = new Mock<UserManager<IdentityUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var service = new PatientService(context, new PatientMapper(), fileStorageService.Object, userManagerMock.Object);

        var dto = new CreateUpdatePatientDto { FirstName = "Nowy", LastName = "Testowy", Pesel = "12312312312" };

        // Act
        var (success, error) = await service.UpdatePatientAsync(patient.Id, dto);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        var updatedPatient = await context.Patients.FindAsync(patient.Id);
        Assert.Equal("Nowy", updatedPatient.FirstName);
    }

    [Fact]
    public async Task LinkOrCreatePatientForUserAsync_ShouldReturnError_WhenUserAlreadyLinked()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var patient = new Patient { FirstName = "Jan", LastName = "Kowalski", Pesel = "11111111111", UserId = "user123" };
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var fileStorageService = new Mock<IFileStorageService>();
        var userStoreMock = new Mock<IUserStore<IdentityUser>>();
        var userManagerMock = new Mock<UserManager<IdentityUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var service = new PatientService(context, new PatientMapper(), fileStorageService.Object, userManagerMock.Object);

        // Act
        var (success, error) = await service.LinkOrCreatePatientForUserAsync("user123", "22222222222", "Adam", "Nowak");

        // Assert
        Assert.False(success);
        Assert.Equal("To konto jest już powiązane z pacjentem.", error);
    }
}