using ClinicManager.Web.Controllers;
using ClinicManager.Web.DTOs;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

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

    [Fact]
    public void Create_Get_ReturnsViewResult()
    {
        // Act
        var result = _controller.Create();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<CreateUpdatePatientDto>(viewResult.Model);
    }

    [Fact]
    public async Task Create_Post_ReturnsRedirectAndCallsService_WhenModelIsValid()
    {
        // Arrange
        var dto = new CreateUpdatePatientDto { FirstName = "Jan", LastName = "Kowalski", Pesel = "12345678901" };
        _mockService.Setup(s => s.CreatePatientAsync(dto)).ReturnsAsync((1, null));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Pacjent został dodany.", _controller.TempData["Success"]);
        _mockService.Verify(s => s.CreatePatientAsync(dto), Times.Once);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenPatientDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetPatientDetailsAsync(It.IsAny<int>())).ReturnsAsync((PatientDetailsDto?)null);

        // Act
        var result = await _controller.Details(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsRedirect_WhenDeletionIsSuccessful()
    {
        // Arrange
        _mockService.Setup(s => s.DeletePatientAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Pacjent oraz powiązane pliki zostały pomyślnie usunięte.", _controller.TempData["Success"]);
        _mockService.Verify(s => s.DeletePatientAsync(1), Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenPatientDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.DeletePatientAsync(99)).ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_ReturnsNotFound_WhenPatientDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetPatientByIdAsync(99)).ReturnsAsync((PatientDto?)null);

        // Act
        var result = await _controller.Edit(99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_ReturnsViewResult_WhenPatientExists()
    {
        // Arrange
        var patient = new PatientDto { Id = 1, FirstName = "Jan", LastName = "Kowalski" };
        _mockService.Setup(s => s.GetPatientByIdAsync(1)).ReturnsAsync(patient);

        // Act
        var result = await _controller.Edit(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<CreateUpdatePatientDto>(viewResult.Model);
    }

    [Fact]
    public async Task Create_Post_ReturnsViewWithError_WhenServiceRejectsDuplicatePesel()
    {
        // Arrange
        var dto = new CreateUpdatePatientDto { FirstName = "Jan", LastName = "Kowalski", Pesel = "12345678901" };
        _mockService.Setup(s => s.CreatePatientAsync(dto)).ReturnsAsync((null, "Pacjent z tym numerem PESEL już istnieje w systemie."));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(dto, viewResult.Model);
        Assert.True(_controller.ModelState.TryGetValue(string.Empty, out var modelState));
        Assert.Contains(modelState!.Errors, e => e.ErrorMessage == "Pacjent z tym numerem PESEL już istnieje w systemie.");
        Assert.Null(_controller.TempData["Success"]);
    }

    [Fact]
    public async Task Create_Post_ReturnsViewWithError_WhenServiceRejectsDuplicateInsuranceNumber()
    {
        // Arrange
        var dto = new CreateUpdatePatientDto { FirstName = "Jan", LastName = "Kowalski", InsuranceNumber = "9999" };
        _mockService.Setup(s => s.CreatePatientAsync(dto)).ReturnsAsync((null, "Pacjent z tym numerem ubezpieczenia już istnieje w systemie."));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(dto, viewResult.Model);

        Assert.True(_controller.ModelState.TryGetValue(string.Empty, out var modelState));
        Assert.Contains(modelState!.Errors, e => e.ErrorMessage == "Pacjent z tym numerem ubezpieczenia już istnieje w systemie.");
    }

    [Fact]
    public async Task Edit_Post_ReturnsViewWithError_WhenServiceRejectsDuplicatePesel()
    {
        // Arrange
        _mockService.Setup(s => s.GetPatientByIdAsync(1)).ReturnsAsync(new PatientDto
        {
            Id = 1,
            FirstName = "Jan",
            LastName = "Kowalski",
            Pesel = "11111111111"
        });
        var dto = new CreateUpdatePatientDto { FirstName = "Jan", LastName = "Kowalski", Pesel = "12345678901" };
        _mockService.Setup(s => s.UpdatePatientAsync(1, dto)).ReturnsAsync((false, "Pacjent z tym numerem PESEL już istnieje w systemie."));

        // Act
        var result = await _controller.Edit(1, dto);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(dto, viewResult.Model);
        Assert.True(_controller.ModelState.TryGetValue(string.Empty, out var modelState));
        Assert.Contains(modelState!.Errors, e => e.ErrorMessage == "Pacjent z tym numerem PESEL już istnieje w systemie.");
        Assert.Null(_controller.TempData["Success"]);
    }
}