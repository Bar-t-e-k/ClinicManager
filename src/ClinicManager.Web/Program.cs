using ClinicManager.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
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
        var config = services.GetRequiredService<IConfiguration>();
        await DataSeeder.SeedRolesAndAdminAsync(services, config);
    }
    catch (Exception ex)
    {
        // TODO: podpięcie NLog (US#2)
        Console.WriteLine($"Błąd podczas seedowania bazy danych: {ex.Message}");
    }
}

app.Run();
