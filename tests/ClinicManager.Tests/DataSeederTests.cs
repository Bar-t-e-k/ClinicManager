using ClinicManager.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClinicManager.Tests;

public class DataSeederTests
{
    [Fact]
    public async Task SeedRolesAndAdminAsync_ShouldCreateRolesAndAllUsers()
    {
        // Arrange
        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        roleManagerMock.Setup(x => x.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        roleManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);

        var userStoreMock = new Mock<IUserStore<IdentityUser>>();
        var userManagerMock = new Mock<UserManager<IdentityUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser)null!);
        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

        var configMock = new Mock<IConfiguration>();
        configMock.SetupGet(x => x["SeedData:AdminPassword"]).Returns("AdminPassword123!");
        configMock.SetupGet(x => x["SeedData:DoctorPassword"]).Returns("DoctorPassword123!");
        configMock.SetupGet(x => x["SeedData:RegPassword"]).Returns("RegPassword123!");

        var loggerMock = new Mock<ILogger<Program>>();

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(RoleManager<IdentityRole>))).Returns(roleManagerMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(UserManager<IdentityUser>))).Returns(userManagerMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(ILogger<Program>))).Returns(loggerMock.Object);

        // Act
        await DataSeeder.SeedRolesAndAdminAsync(serviceProviderMock.Object, configMock.Object);

        // Assert
        roleManagerMock.Verify(x => x.CreateAsync(It.IsAny<IdentityRole>()), Times.Exactly(3));

        userManagerMock.Verify(x => x.FindByEmailAsync("admin@clinic.com"), Times.Once);
        userManagerMock.Verify(x => x.FindByEmailAsync("lekarz@clinic.com"), Times.Once);
        userManagerMock.Verify(x => x.FindByEmailAsync("rejestracja@clinic.com"), Times.Once);

        userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), "Admin"), Times.Once);
        userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), "Lekarz"), Times.Once);
        userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), "Rejestratorka"), Times.Once);
    }
}