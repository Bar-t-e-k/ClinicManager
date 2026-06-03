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
    private readonly Mock<IFileStorageService> _mockFileStorage;
    private readonly PatientsController _controller;

    public PatientsControllerTests()
    {
        _mockService = new Mock<IPatientService>();
        _mockFileStorage = new Mock<IFileStorageService>();

        _controller = new PatientsController(_mockService.Object, _mockFileStorage.Object)
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
        string fakePath = "/uploads/fake-file.pdf";

        _mockService.Setup(s => s.DeleteMedicalRecordAsync(recordId, patientId))
            .ReturnsAsync(fakePath);

        _mockFileStorage.Setup(s => s.DeleteFile(fakePath));

        // Act
        var result = await _controller.DeleteDocument(recordId, patientId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal(patientId, redirectResult.RouteValues["id"]);

        _mockFileStorage.Verify(s => s.DeleteFile(fakePath), Times.Once);
    }

    [Fact]
    public async Task UploadDocument_RedirectsToDetails_WithTempDataError_WhenExtensionIsInvalid()
    {
        // Arrange
        int patientId = 1;

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("zly_program.exe");
        mockFile.Setup(f => f.Length).Returns(1024);

        // Act
        var result = await _controller.UploadDocument(patientId, mockFile.Object) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Details", result.ActionName);
        Assert.Equal(patientId, result.RouteValues["id"]);

        Assert.Equal("Niedozwolony format pliku. Wgraj plik PDF, JPG lub PNG.", _controller.TempData["Error"]);

        _mockService.Verify(s => s.AddMedicalRecordAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UploadDocument_SavesFile_AndCallsService_WhenFileIsValid()
    {
        // Arrange
        int patientId = 1;
        string validFileName = "wyniki_badan.pdf";
        string fakeSavedPath = "/uploads/" + validFileName;

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(validFileName);
        mockFile.Setup(f => f.Length).Returns(1024);

        _mockFileStorage.Setup(s => s.SaveFileAsync(mockFile.Object, It.IsAny<string>()))
            .ReturnsAsync(fakeSavedPath);

        _mockService.Setup(s => s.AddMedicalRecordAsync(patientId, validFileName, fakeSavedPath))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UploadDocument(patientId, mockFile.Object) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Details", result.ActionName);

        Assert.Equal("Dokument został wgrany.", _controller.TempData["Success"]);

        _mockFileStorage.Verify(s => s.SaveFileAsync(mockFile.Object, It.IsAny<string>()), Times.Once);
        _mockService.Verify(s => s.AddMedicalRecordAsync(patientId, validFileName, fakeSavedPath), Times.Once);
    }
}