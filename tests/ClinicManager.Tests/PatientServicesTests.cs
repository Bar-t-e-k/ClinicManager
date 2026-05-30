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
    [Fact]
    public async Task AddMedicalRecordAsync_ShouldAddRecordToDatabase()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var patient = new Patient { FirstName = "Jan", LastName = "Plikowy", Pesel = "00000000000" };
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var service = new PatientService(context);

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

        var service = new PatientService(context);

        // Act
        var returnedPath = await service.DeleteMedicalRecordAsync(record.Id, patient.Id);

        // Assert
        Assert.Equal("/uploads/do-usuniecia.jpg", returnedPath); 
        var recordsInDb = await context.Set<MedicalRecord>().ToListAsync();
        Assert.Empty(recordsInDb); 
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

    private static async Task<(Patient patient, IdentityUser doctor)> SeedVisitPrerequisitesAsync(ClinicDbContext context)
    {
        var patient = new Patient { FirstName = "Jan", LastName = "Kowalski", Pesel = "12345678901" };
        context.Patients.Add(patient);

        var doctor = new IdentityUser { Id = "doc1", UserName = "lekarz@clinic.com", Email = "lekarz@clinic.com" };
        context.Users.Add(doctor);

        await context.SaveChangesAsync();
        return (patient, doctor);
    }

    [Fact]
    public async Task CreateVisitAsync_ShouldFail_WhenDateIsInThePast()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var (patient, doctor) = await SeedVisitPrerequisitesAsync(context);
        var userManager = GetMockUserManager(doctor);
        var service = new VisitService(context, userManager);

        var dto = new CreateVisitDto
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
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
        var (patient, doctor) = await SeedVisitPrerequisitesAsync(context);
        var userManager = GetMockUserManager(doctor);
        var service = new VisitService(context, userManager);

        var dto = new CreateVisitDto
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
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
        var (patient, doctor) = await SeedVisitPrerequisitesAsync(context);
        var userManager = GetMockUserManager(doctor);
        var service = new VisitService(context, userManager);

        var visit = new Visit
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
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
        var (patient, doctor) = await SeedVisitPrerequisitesAsync(context);
        var userManager = GetMockUserManager(doctor);
        var service = new VisitService(context, userManager);

        var visit = new Visit
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
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
    public async Task DeleteClinicalNoteAsync_ShouldFail_WhenNoteDoesNotBelongToVisit()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var (patient, doctor) = await SeedVisitPrerequisitesAsync(context);
        var userManager = GetMockUserManager(doctor);
        var service = new VisitService(context, userManager);

        var visit1 = new Visit { PatientId = patient.Id, DoctorId = doctor.Id, ScheduledDate = DateTime.Today.AddDays(1) };
        var visit2 = new Visit { PatientId = patient.Id, DoctorId = doctor.Id, ScheduledDate = DateTime.Today.AddDays(2) };
        context.Visits.AddRange(visit1, visit2);
        await context.SaveChangesAsync();

        var note = new ClinicalNote { VisitId = visit1.Id, Content = "Notatka do wizyty 1." };
        context.ClinicalNotes.Add(note);
        await context.SaveChangesAsync();

        // Act — próba usunięcia notatki z wizyty 1 podając ID wizyty 2
        var result = await service.DeleteClinicalNoteAsync(note.Id, visit2.Id);

        // Assert
        Assert.False(result);
        Assert.Equal(1, await context.ClinicalNotes.CountAsync());
    }

    [Fact]
    public async Task AddMedicationAsync_ShouldIncreaseTotalCost()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var (patient, doctor) = await SeedVisitPrerequisitesAsync(context);
        var userManager = GetMockUserManager(doctor);
        var service = new VisitService(context, userManager);

        var visit = new Visit
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
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
        var (patient, doctor) = await SeedVisitPrerequisitesAsync(context);
        var userManager = GetMockUserManager(doctor);
        var service = new VisitService(context, userManager);

        var visit = new Visit
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
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

    [Fact]
    public async Task UpdateVisitStatusAsync_ShouldFail_WhenStatusIsInvalid()
    {
        // Arrange
        var context = await GetInMemoryDbContextAsync();
        var (patient, doctor) = await SeedVisitPrerequisitesAsync(context);
        var userManager = GetMockUserManager(doctor);
        var service = new VisitService(context, userManager);

        var visit = new Visit
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            ScheduledDate = DateTime.Today.AddDays(1),
            Status = VisitStatus.Zaplanowana
        };
        context.Visits.Add(visit);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateVisitStatusAsync(visit.Id, (VisitStatus)999);

        // Assert
        Assert.False(result);
    }
}