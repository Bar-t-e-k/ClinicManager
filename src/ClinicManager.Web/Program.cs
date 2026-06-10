using ClinicManager.Web.BackgroundServices;
using ClinicManager.Web.Configuration;
using ClinicManager.Web.Data;
using ClinicManager.Web.Filters;
using ClinicManager.Web.Mappers;
using ClinicManager.Web.Services;
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
    {
        options.UseSqlServer(connectionString);
        if (builder.Configuration.GetValue("EfCore:LogSqlToConsole", false))
            options.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
    });

    // 2. Rejestracja systemu Identity
    builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 4;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
        .AddEntityFrameworkStores<ClinicDbContext>()
        .AddDefaultUI()
        .AddDefaultTokenProviders();

    // 3. Rejestracja serwisów aplikacji
    builder.Services.AddScoped<IPatientService, PatientService>();
    builder.Services.AddScoped<IVisitService, VisitService>();
    builder.Services.AddScoped<IMedicationService, MedicationService>();
    builder.Services.AddScoped<IProcedureService, ProcedureService>();
    builder.Services.AddScoped<IUpcomingVisitsReportService, UpcomingVisitsReportService>();
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

    builder.Services.Configure<UpcomingVisitsReportOptions>(
        builder.Configuration.GetSection(UpcomingVisitsReportOptions.SectionName));
    if (builder.Configuration.GetValue("UpcomingVisitsReport:Enabled", true))
        builder.Services.AddHostedService<UpcomingVisitsReportBackgroundService>();

    // 4. Rejestracja kontrolerów oraz dodanie globalnego filtra wyjątków
    builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.Add<GlobalExceptionFilter>();
    });

    builder.Services.AddRazorPages();

    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(c =>
    {
        var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    });

    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
    builder.Services.AddScoped<IPatientMapper, PatientMapper>();
    builder.Services.AddScoped<IReportService, ReportService>();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "ClinicManager API v1");
        });
    }
    else
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
    app.MapControllers();

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

    var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
    startupLogger.LogInformation(
        "Aplikacja działa. Urls: {Urls} | Swagger: /swagger",
        string.Join(", ", app.Urls));

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    logger.Error(ex, "Aplikacja zakończyła działanie z powodu nieobsłużonego wyjątku.");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}