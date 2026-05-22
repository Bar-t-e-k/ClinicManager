using ClinicManager.Web.Controllers;
using ClinicManager.Web.DTOs;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ClinicManager.Tests;

public class PatientsControllerTests
{
    [Fact]
    public async Task Index_ReturnsViewResult_WithListOfPatients()
    {
        // Arrange
        var mockService = new Mock<IPatientService>();

        var fakePatients = new List<PatientDto>
        {
            new PatientDto { Id = 1, FirstName = "Jan", LastName = "Kowalski", Pesel = "12345678901" },
            new PatientDto { Id = 2, FirstName = "Anna", LastName = "Nowak", Pesel = "11122233344" }
        };

        mockService
            .Setup(service => service.GetAllPatientsAsync(It.IsAny<string>()))
            .ReturnsAsync(fakePatients);

        var controller = new PatientsController(mockService.Object);
        var searchTerm = "Kowalski";

        // Act
        var result = await controller.Index(searchTerm);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);

        var model = Assert.IsAssignableFrom<IEnumerable<PatientDto>>(viewResult.Model);
        Assert.Equal(2, model.Count());

        Assert.Equal(searchTerm, viewResult.ViewData["CurrentFilter"]);
    }
}