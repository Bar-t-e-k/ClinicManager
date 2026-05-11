using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Web.Data;

public class ClinicDbContext : IdentityDbContext<IdentityUser>
{
    public ClinicDbContext(DbContextOptions<ClinicDbContext> options) : base(options)
    {
    }

    // TODO
    // public DbSet<Patient> Patients { get; set; }
    // public DbSet<Visit> Visits { get; set; }
}