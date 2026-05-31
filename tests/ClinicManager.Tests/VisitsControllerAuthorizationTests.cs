using System.Security.Claims;
using ClinicManager.Web.Controllers;
using ClinicManager.Web.DTOs;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace ClinicManager.Tests;

public class VisitsControllerAuthorizationTests
{
    private readonly Mock<IVisitService> _visitService = new();
    private readonly Mock<IPatientService> _patientService = new();
    private readonly Mock<IMedicationService> _medicationService = new();
    private readonly Mock<UserManager<IdentityUser>> _userManager;

    private const string OwnDoctorId = "doctor-own";
    private const string OtherDoctorId = "doctor-other";

    public VisitsControllerAuthorizationTests()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        _userManager = new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task AddNote_ReturnsForbid_WhenDoctorDoesNotOwnVisit()
    {
        var controller = CreateControllerAsDoctor(OwnDoctorId);
        _visitService.Setup(s => s.GetVisitDoctorIdAsync(1)).ReturnsAsync(OtherDoctorId);

        var result = await controller.AddNote(1, new CreateClinicalNoteDto { Content = "Testowa notatka." });

        Assert.IsType<ForbidResult>(result);
        _visitService.Verify(s => s.AddClinicalNoteAsync(It.IsAny<int>(), It.IsAny<CreateClinicalNoteDto>()), Times.Never);
    }

    [Fact]
    public async Task AddNote_RedirectsToDetails_WhenDoctorOwnsVisit()
    {
        var controller = CreateControllerAsDoctor(OwnDoctorId);
        _visitService.Setup(s => s.GetVisitDoctorIdAsync(1)).ReturnsAsync(OwnDoctorId);
        _visitService.Setup(s => s.AddClinicalNoteAsync(1, It.IsAny<CreateClinicalNoteDto>())).ReturnsAsync(true);

        var result = await controller.AddNote(1, new CreateClinicalNoteDto { Content = "Testowa notatka." });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        _visitService.Verify(s => s.AddClinicalNoteAsync(1, It.IsAny<CreateClinicalNoteDto>()), Times.Once);
    }

    [Fact]
    public async Task AddMedication_ReturnsForbid_WhenDoctorDoesNotOwnVisit()
    {
        var controller = CreateControllerAsDoctor(OwnDoctorId);
        _visitService.Setup(s => s.GetVisitDoctorIdAsync(5)).ReturnsAsync(OtherDoctorId);

        var result = await controller.AddMedication(5, new AddMedicationToVisitDto { MedicationId = 1, Quantity = 1 });

        Assert.IsType<ForbidResult>(result);
        _visitService.Verify(s => s.AddMedicationAsync(It.IsAny<int>(), It.IsAny<AddMedicationToVisitDto>()), Times.Never);
    }

    [Fact]
    public async Task DeleteNote_ReturnsForbid_WhenDoctorDoesNotOwnVisit()
    {
        var controller = CreateControllerAsDoctor(OwnDoctorId);
        _visitService.Setup(s => s.GetVisitDoctorIdAsync(2)).ReturnsAsync(OtherDoctorId);

        var result = await controller.DeleteNote(10, 2);

        Assert.IsType<ForbidResult>(result);
        _visitService.Verify(s => s.DeleteClinicalNoteAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RemoveMedication_ReturnsForbid_WhenDoctorDoesNotOwnVisit()
    {
        var controller = CreateControllerAsDoctor(OwnDoctorId);
        _visitService.Setup(s => s.GetVisitDoctorIdAsync(3)).ReturnsAsync(OtherDoctorId);

        var result = await controller.RemoveMedication(99, 3);

        Assert.IsType<ForbidResult>(result);
        _visitService.Verify(s => s.RemoveMedicationAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AddNote_ReturnsNotFound_WhenVisitDoesNotExist()
    {
        var controller = CreateControllerAsDoctor(OwnDoctorId);
        _visitService.Setup(s => s.GetVisitDoctorIdAsync(404)).ReturnsAsync((string?)null);

        var result = await controller.AddNote(404, new CreateClinicalNoteDto { Content = "Test." });

        Assert.IsType<NotFoundResult>(result);
    }

    private VisitsController CreateControllerAsDoctor(string doctorId)
    {
        var doctor = new IdentityUser { Id = doctorId, UserName = "lekarz@test.com", Email = "lekarz@test.com" };
        _userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(doctor);

        var controller = new VisitsController(
            _visitService.Object,
            _patientService.Object,
            _medicationService.Object,
            _userManager.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, doctorId),
                        new Claim(ClaimTypes.Role, "Lekarz")
                    ], "Test"))
                }
            },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };

        return controller;
    }
}
