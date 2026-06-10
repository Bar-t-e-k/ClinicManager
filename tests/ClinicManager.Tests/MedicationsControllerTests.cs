using ClinicManager.Web.Controllers;
using ClinicManager.Web.DTOs;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace ClinicManager.Tests;

public class MedicationsControllerTests
{
    private readonly Mock<IMedicationService> _mockService;
    private readonly MedicationsController _controller;

    public MedicationsControllerTests()
    {
        _mockService = new Mock<IMedicationService>();
        _controller = new MedicationsController(_mockService.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithMedications()
    {
        // Arrange
        var medications = new List<CreateUpdateMedicationDto> { new CreateUpdateMedicationDto { Id = 1, Name = "Lek 1" } };
        _mockService.Setup(s => s.GetAllMedicationsAsync()).ReturnsAsync(medications);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<CreateUpdateMedicationDto>>(viewResult.ViewData.Model);
        Assert.Single(model);
    }

    [Fact]
    public void Create_Get_ReturnsViewResult()
    {
        // Act
        var result = _controller.Create();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Create_Post_ReturnsView_WhenModelStateIsInvalid()
    {
        // Arrange
        _controller.ModelState.AddModelError("Name", "Required");
        var dto = new CreateUpdateMedicationDto();

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(dto, viewResult.Model);
        _mockService.Verify(s => s.CreateMedicationAsync(It.IsAny<CreateUpdateMedicationDto>()), Times.Never);
    }

    [Fact]
    public async Task Create_Post_RedirectsToIndex_WhenSuccessful()
    {
        // Arrange
        var dto = new CreateUpdateMedicationDto { Name = "Nowy Lek" };
        _mockService.Setup(s => s.CreateMedicationAsync(dto)).ReturnsAsync(1);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Lek został dodany do katalogu.", _controller.TempData["Success"]);
    }

    [Fact]
    public async Task Edit_Get_ReturnsNotFound_WhenIdDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetMedicationByIdAsync(99)).ReturnsAsync((CreateUpdateMedicationDto?)null);

        // Act
        var result = await _controller.Edit(99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_ReturnsViewResult_WhenIdExists()
    {
        // Arrange
        var dto = new CreateUpdateMedicationDto { Id = 1, Name = "Lek 1" };
        _mockService.Setup(s => s.GetMedicationByIdAsync(1)).ReturnsAsync(dto);

        // Act
        var result = await _controller.Edit(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<CreateUpdateMedicationDto>(viewResult.Model);
    }

    [Fact]
    public async Task Edit_Post_RedirectsToIndex_WhenSuccessful()
    {
        // Arrange
        var dto = new CreateUpdateMedicationDto { Id = 1, Name = "Lek Zaktualizowany" };
        _mockService.Setup(s => s.UpdateMedicationAsync(1, dto)).ReturnsAsync(true);

        // Act
        var result = await _controller.Edit(1, dto);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Dane leku zostały zaktualizowane.", _controller.TempData["Success"]);
    }

    [Fact]
    public async Task Deactivate_RedirectsToIndex()
    {
        // Arrange
        _mockService.Setup(s => s.DeactivateMedicationAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.Deactivate(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Lek został dezaktywowany.", _controller.TempData["Success"]);
    }
}