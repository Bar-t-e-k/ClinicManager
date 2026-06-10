using ClinicManager.Web.Controllers;
using ClinicManager.Web.DTOs;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace ClinicManager.Tests;

public class ProceduresControllerTests
{
    private readonly Mock<IProcedureService> _mockService;
    private readonly ProceduresController _controller;

    public ProceduresControllerTests()
    {
        _mockService = new Mock<IProcedureService>();
        _controller = new ProceduresController(_mockService.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithProcedures()
    {
        // Arrange
        var procedures = new List<CreateUpdateProcedureDto> { new CreateUpdateProcedureDto { Id = 1, Description = "Proc 1" } };
        _mockService.Setup(s => s.GetAllProceduresAsync()).ReturnsAsync(procedures);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<CreateUpdateProcedureDto>>(viewResult.ViewData.Model);
        Assert.Single(model);
    }

    [Fact]
    public async Task Create_Post_RedirectsToIndex_WhenSuccessful()
    {
        // Arrange
        var dto = new CreateUpdateProcedureDto { Description = "Nowa procedura" };
        _mockService.Setup(s => s.CreateProcedureAsync(dto)).ReturnsAsync(1);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Procedura została dodana do katalogu.", _controller.TempData["Success"]);
    }

    [Fact]
    public async Task Edit_Get_ReturnsNotFound_WhenIdDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetProcedureByIdAsync(99)).ReturnsAsync((CreateUpdateProcedureDto?)null);

        // Act
        var result = await _controller.Edit(99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_ReturnsNotFound_WhenUpdateFails()
    {
        // Arrange
        var dto = new CreateUpdateProcedureDto { Id = 99, Description = "X" };
        _mockService.Setup(s => s.UpdateProcedureAsync(99, dto)).ReturnsAsync(false);

        // Act
        var result = await _controller.Edit(99, dto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Deactivate_RedirectsToIndex()
    {
        // Arrange
        _mockService.Setup(s => s.DeactivateProcedureAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.Deactivate(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Procedura została dezaktywowana.", _controller.TempData["Success"]);
    }
}