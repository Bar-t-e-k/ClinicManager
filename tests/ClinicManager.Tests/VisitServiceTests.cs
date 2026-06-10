using ClinicManager.Web.Data;
using ClinicManager.Web.DTOs;
using ClinicManager.Web.Mappers;
using ClinicManager.Web.Models;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClinicManager.Tests
{
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

        private static async Task<(Patient patient, IdentityUser doctor)> SeedVisitPrerequisitesAsync(
            ClinicDbContext context)
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
        public async Task CreatePatientAsync_ShouldFail_WhenPeselAlreadyExists()
        {
            var context = await GetInMemoryDbContextAsync();
            context.Patients.Add(new Patient { FirstName = "Jan", LastName = "Kowalski", Pesel = "12345678901" });
            await context.SaveChangesAsync();

            var fileStorageService = new Mock<IFileStorageService>();
            var userStoreMock = new Mock<IUserStore<IdentityUser>>();
            var userManagerMock = new Mock<UserManager<IdentityUser>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            var service = new PatientService(context, new PatientMapper(), fileStorageService.Object, userManagerMock.Object);
            var (patientId, error) = await service.CreatePatientAsync(new CreateUpdatePatientDto
            {
                FirstName = "Adam",
                LastName = "Nowak",
                Pesel = "12345678901"
            });

            Assert.Null(patientId);
            Assert.NotNull(error);
        }

        [Fact]
        public async Task GetActiveVisitsAsync_ShouldReturnOnlyActiveStatuses()
        {
            var context = await GetInMemoryDbContextAsync();
            var (patient, doctor) = await SeedVisitPrerequisitesAsync(context);
            var userManager = GetMockUserManager(doctor);
            var service = new VisitService(context, userManager);

            context.Visits.AddRange(
                new Visit
                {
                    PatientId = patient.Id, DoctorId = doctor.Id, ScheduledDate = DateTime.Today.AddDays(1),
                    Status = VisitStatus.Zaplanowana
                },
                new Visit
                {
                    PatientId = patient.Id, DoctorId = doctor.Id, ScheduledDate = DateTime.Today.AddDays(2),
                    Status = VisitStatus.Zakonczona
                },
                new Visit
                {
                    PatientId = patient.Id, DoctorId = doctor.Id, ScheduledDate = DateTime.Today.AddDays(3),
                    Status = VisitStatus.Anulowana
                });
            await context.SaveChangesAsync();

            var result = await service.GetActiveVisitsAsync();

            Assert.Single(result);
            Assert.Equal("Zaplanowana", result[0].Status);
            Assert.Equal(patient.Id, result[0].PatientId);
        }

        [Fact]
        public async Task CancelVisitAsync_ShouldSetStatusToAnulowana()
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
            var result = await service.CancelVisitAsync(visit.Id);

            // Assert
            Assert.True(result);
            var visitInDb = await context.Visits.FindAsync(visit.Id);
            Assert.False(visitInDb!.IsDeleted);
            Assert.Equal(VisitStatus.Anulowana, visitInDb.Status);
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

            var visit1 = new Visit
                { PatientId = patient.Id, DoctorId = doctor.Id, ScheduledDate = DateTime.Today.AddDays(1) };
            var visit2 = new Visit
                { PatientId = patient.Id, DoctorId = doctor.Id, ScheduledDate = DateTime.Today.AddDays(2) };
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

        [Fact]
        public async Task AddProcedureAsync_ShouldAddProcedureAndIncreaseTotalCost()
        {
            // Arrange
            var context = await GetInMemoryDbContextAsync();
            var (patient, doctor) = await SeedVisitPrerequisitesAsync(context);
            var userManager = GetMockUserManager(doctor);
            var service = new VisitService(context, userManager);

            var visit = new Visit { PatientId = patient.Id, DoctorId = doctor.Id, ScheduledDate = DateTime.Today.AddDays(1) };
            context.Visits.Add(visit);

            var procedure = new Procedure { Description = "Konsultacja", Cost = 150.00m, IsActive = true };
            context.Procedures.Add(procedure);
            await context.SaveChangesAsync();

            var dto = new AddProcedureToVisitDto { ProcedureId = procedure.Id, Quantity = 1 };

            // Act
            var (success, error) = await service.AddProcedureAsync(visit.Id, dto);

            // Assert
            Assert.True(success);
            var visitInDb = await context.Visits.Include(v => v.VisitProcedures).FirstOrDefaultAsync(v => v.Id == visit.Id);
            Assert.Single(visitInDb.VisitProcedures);
            Assert.Equal(150.00m, visitInDb.TotalCost);
        }

        [Fact]
        public async Task RemoveMedicationAsync_ShouldRemoveMedicationAndDecreaseTotalCost()
        {
            // Arrange
            var context = await GetInMemoryDbContextAsync();
            var (patient, doctor) = await SeedVisitPrerequisitesAsync(context);
            var userManager = GetMockUserManager(doctor);
            var service = new VisitService(context, userManager);

            var visit = new Visit { PatientId = patient.Id, DoctorId = doctor.Id, ScheduledDate = DateTime.Today.AddDays(1), TotalCost = 100.00m };
            var visitMedication = new VisitMedication { Visit = visit, MedicationId = 1, Quantity = 2, UnitPrice = 50.00m };
            visit.VisitMedications.Add(visitMedication);
            context.Visits.Add(visit);
            await context.SaveChangesAsync();

            // Act
            var result = await service.RemoveMedicationAsync(visitMedication.Id);

            // Assert
            Assert.True(result);
            var visitInDb = await context.Visits.Include(v => v.VisitMedications).FirstOrDefaultAsync(v => v.Id == visit.Id);
            Assert.Empty(visitInDb.VisitMedications);
            Assert.Equal(0.00m, visitInDb.TotalCost);
        }

        [Fact]
        public async Task GetAllVisitsAsync_ShouldReturnAllNotDeletedVisits()
        {
            // Arrange
            var context = await GetInMemoryDbContextAsync();
            var (patient, doctor) = await SeedVisitPrerequisitesAsync(context);
            var userManager = GetMockUserManager(doctor);
            var service = new VisitService(context, userManager);

            context.Visits.AddRange(
                new Visit { PatientId = patient.Id, DoctorId = doctor.Id, ScheduledDate = DateTime.Today.AddDays(1), IsDeleted = false },
                new Visit { PatientId = patient.Id, DoctorId = doctor.Id, ScheduledDate = DateTime.Today.AddDays(2), IsDeleted = true } 
            );
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetAllVisitsAsync(null);

            // Assert
            Assert.Single(result);
        }
    }
}
