using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using ClinicManager.Web.Models;

namespace ClinicManager.Web.Data;

public class ClinicDbContext : IdentityDbContext<IdentityUser>
{
    public ClinicDbContext(DbContextOptions<ClinicDbContext> options) : base(options)
    {
    }

    public DbSet<Patient> Patients { get; set; }

    // TODO
    // public DbSet<Visit> Visits { get; set; }
}