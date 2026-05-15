using System.Reflection.Metadata;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NLog;

namespace ClinicManager.Web.Data;

public static class DataSeeder
{
    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            string[] roleNames = { "Admin", "Lekarz", "Rejestratorka" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            var adminEmail = "admin@clinic.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                string password = configuration["SeedData:AdminPassword"] ??
                                  throw new InvalidOperationException(
                                      "Hasło administratora nie zostało skonfigurowane w User Secrets!");
                var createPowerUser = await userManager.CreateAsync(newAdmin, password);

                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }

            logger.LogInformation("Pomyślnie wykonano seedowanie bazy danych.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Błąd podczas seedowania danych: {Message}", ex.Message);
            throw;
        }
    }
}