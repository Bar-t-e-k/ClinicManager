using ClinicManager.Web.Controllers;
using ClinicManager.Web.DTOs;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace ClinicManager.Tests;

public class PatientsControllerTests
{
    private readonly Mock<IPatientService> _mockService;
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly PatientsController _controller;

    public PatientsControllerTests()
    {
        _mockService = new Mock<IPatientService>();
        _mockEnv = new Mock<IWebHostEnvironment>();

        _mockEnv.Setup(m => m.WebRootPath).Returns("C:\\temp");

        _controller = new PatientsController(_mockService.Object, _mockEnv.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithListOfPatients()
    {
        // Arrange
        var fakePatients = new List<PatientDto>
        {
            new PatientDto { Id = 1, FirstName = "Jan", LastName = "Kowalski", Pesel = "12345678901" },
            new PatientDto { Id = 2, FirstName = "Anna", LastName = "Nowak", Pesel = "11122233344" }
        };

        _mockService
            .Setup(service => service.GetAllPatientsAsync(It.IsAny<string>()))
            .ReturnsAsync(fakePatients);

        var searchTerm = "Kowalski";

        // Act
        var result = await _controller.Index(searchTerm);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<PatientDto>>(viewResult.Model);
        Assert.Equal(2, model.Count());
        Assert.Equal(searchTerm, viewResult.ViewData["CurrentFilter"]);
    }

    [Fact]
    public async Task UploadDocument_RedirectsToDetails_WithTempDataError_WhenFileIsNull()
    {
        // Arrange
        int patientId = 1;
        IFormFile nullFile = null!;

        // Act
        var result = await _controller.UploadDocument(patientId, nullFile);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal(patientId, redirectResult.RouteValues["id"]);
        Assert.Equal("Wybierz plik przed wysłaniem.", _controller.TempData["Error"]);
    }

    [Fact]
    public async Task DeleteDocument_RedirectsToDetails_WhenCalled()
    {
        // Arrange
        int patientId = 1;
        int recordId = 10;

        _mockService.Setup(s => s.DeleteMedicalRecordAsync(recordId))
            .ReturnsAsync("/uploads/fake-file.pdf");

        // Act
        var result = await _controller.DeleteDocument(recordId, patientId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal(patientId, redirectResult.RouteValues["id"]);
    }
}