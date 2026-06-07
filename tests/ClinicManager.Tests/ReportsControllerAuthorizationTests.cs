using System.Security.Claims;
using ClinicManager.Web.Controllers;
using ClinicManager.Web.Data;
using ClinicManager.Web.Models;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ClinicManager.Tests;

public class ReportsControllerAuthorizationTests
{
    private readonly Mock<IReportService> _reportService = new();
    private readonly Mock<UserManager<IdentityUser>> _userManager;

    public ReportsControllerAuthorizationTests()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        _userManager = new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task DownloadPatientMonthlyReport_ReturnsFile_WhenPatientRequestsOwnReport()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var patient = await SeedPatientAsync(context, userId: "patient-1", isDeleted: false);

        var controller = CreateController(context, CreatePrincipal("patient-1", "Pacjent"));
        _reportService.Setup(s => s.GeneratePatientMonthlyCostReportAsync(patient.Id, 2026, 5))
            .ReturnsAsync([1, 2, 3]);

        // Act
        var result = await controller.DownloadPatientMonthlyReport(patient.Id, 2026, 5);

        // Assert
        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        _reportService.Verify(s => s.GeneratePatientMonthlyCostReportAsync(patient.Id, 2026, 5), Times.Once);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Rejestratorka")]
    public async Task DownloadPatientMonthlyReport_ReturnsFile_WhenStaffRequestsAnyPatient(string role)
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var patient = await SeedPatientAsync(context, userId: "patient-2", isDeleted: false);

        var controller = CreateController(context, CreatePrincipal("staff-1", role));
        _reportService.Setup(s => s.GeneratePatientMonthlyCostReportAsync(patient.Id, 2026, 5))
            .ReturnsAsync([1, 2, 3]);

        // Act
        var result = await controller.DownloadPatientMonthlyReport(patient.Id, 2026, 5);

        // Assert
        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        _reportService.Verify(s => s.GeneratePatientMonthlyCostReportAsync(patient.Id, 2026, 5), Times.Once);
    }

    [Fact]
    public async Task DownloadPatientMonthlyReport_ReturnsForbid_WhenUserHasNoPermission()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var patient = await SeedPatientAsync(context, userId: "patient-3", isDeleted: false);

        var controller = CreateController(context, CreatePrincipal("random-user"));

        // Act
        var result = await controller.DownloadPatientMonthlyReport(patient.Id, 2026, 5);

        // Assert
        Assert.IsType<ForbidResult>(result);
        _reportService.Verify(s => s.GeneratePatientMonthlyCostReportAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    private ReportsController CreateController(ClinicDbContext context, ClaimsPrincipal user)
    {
        return new ReportsController(_reportService.Object, context, _userManager.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    private static ClaimsPrincipal CreatePrincipal(string userId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static async Task<ClinicDbContext> CreateInMemoryContextAsync()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ClinicDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static async Task<Patient> SeedPatientAsync(ClinicDbContext context, string? userId, bool isDeleted)
    {
        var patient = new Patient
        {
            FirstName = "Jan",
            LastName = "Kowalski",
            Pesel = "12345678901",
            UserId = userId,
            IsDeleted = isDeleted
        };

        context.Patients.Add(patient);
        await context.SaveChangesAsync();
        return patient;
    }
}