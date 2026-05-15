using ClinicManager.Web.Data;
using ClinicManager.Web.Filters;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("Aplikacja startuje...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // 1. Rejestracja kontekstu bazy danych z użyciem SQL Server
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<ClinicDbContext>(options =>
        options.UseSqlServer(connectionString));

    // 2. Rejestracja systemu Identity
    builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            // Opcjonalne ułatwienie na czas dewelopmentu
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 4;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
        })
        .AddEntityFrameworkStores<ClinicDbContext>()
        .AddDefaultUI()
        .AddDefaultTokenProviders();

    // Rejestracja kontrolerów oraz dodanie globalnego filtra wyjątków
    builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.Add<GlobalExceptionFilter>();
    });

    builder.Services.AddRazorPages();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.MapRazorPages();

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ClinicDbContext>();

            await context.Database.MigrateAsync();

            var config = services.GetRequiredService<IConfiguration>();
            await DataSeeder.SeedRolesAndAdminAsync(services, config);
        }
        catch (Exception ex)
        {
            var diLogger = services.GetRequiredService<ILogger<Program>>();
            diLogger.LogError(ex, "Błąd podczas seedowania bazy danych.");
        }
    }

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Aplikacja zakończyła działanie z powodu nieobsłużonego wyjątku.");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}