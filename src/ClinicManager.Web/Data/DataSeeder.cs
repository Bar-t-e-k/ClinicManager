using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

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
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // Admin
            var adminEmail = "admin@clinic.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var newAdmin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                string password = configuration["SeedData:AdminPassword"] ??
                                  throw new InvalidOperationException("Hasło administratora nie zostało skonfigurowane w User Secrets!");

                var result = await userManager.CreateAsync(newAdmin, password);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
            }

            // Lekarz testowy
            var doctorEmail = "lekarz@clinic.com";
            if (await userManager.FindByEmailAsync(doctorEmail) == null)
            {
                var newDoctor = new IdentityUser
                {
                    UserName = doctorEmail,
                    Email = doctorEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newDoctor, "Lekarz123!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(newDoctor, "Lekarz");
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