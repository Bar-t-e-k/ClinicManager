using ClinicManager.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Web.Data;

public class ClinicDbContext : IdentityDbContext<IdentityUser>
{
    public ClinicDbContext(DbContextOptions<ClinicDbContext> options) : base(options) {}

    public DbSet<Patient> Patients { get; set; }
    public DbSet<Visit> Visits { get; set; }
    public DbSet<ClinicalNote> ClinicalNotes { get; set; }
    public DbSet<Medication> Medications { get; set; }
    public DbSet<VisitMedication> VisitMedications { get; set; }
    public DbSet<MedicalRecord> MedicalRecords { get; set; }
    public DbSet<Procedure> Procedures { get; set; }
    public DbSet<VisitProcedure> VisitProcedures { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Patient>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Visit>().HasQueryFilter(v => !v.IsDeleted && !v.Patient.IsDeleted);
        modelBuilder.Entity<MedicalRecord>().HasQueryFilter(m => !m.Patient.IsDeleted);
        modelBuilder.Entity<ClinicalNote>().HasQueryFilter(c => !c.Visit.IsDeleted && !c.Visit.Patient.IsDeleted);
        modelBuilder.Entity<VisitMedication>().HasQueryFilter(vm => !vm.Visit.IsDeleted && !vm.Visit.Patient.IsDeleted);
        modelBuilder.Entity<VisitProcedure>().HasQueryFilter(vp => !vp.Visit.IsDeleted && !vp.Visit.Patient.IsDeleted);

        // Visit -> Patient (restrict delete so we don't lose history)
        modelBuilder.Entity<Visit>()
            .HasOne(v => v.Patient)
            .WithMany()
            .HasForeignKey(v => v.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Visit -> IdentityUser (Doctor)
        modelBuilder.Entity<Visit>()
            .HasOne(v => v.Doctor)
            .WithMany()
            .HasForeignKey(v => v.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Visit -> ClinicalNote (cascade: deleting a visit deletes its notes)
        modelBuilder.Entity<ClinicalNote>()
            .HasOne(n => n.Visit)
            .WithMany(v => v.ClinicalNotes)
            .HasForeignKey(n => n.VisitId)
            .OnDelete(DeleteBehavior.Cascade);

        // VisitMedication joins
        modelBuilder.Entity<VisitMedication>()
            .HasOne(vm => vm.Visit)
            .WithMany(v => v.VisitMedications)
            .HasForeignKey(vm => vm.VisitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VisitMedication>()
            .HasOne(vm => vm.Medication)
            .WithMany(m => m.VisitMedications)
            .HasForeignKey(vm => vm.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);

        // VisitProcedure joins (US#12)
        modelBuilder.Entity<VisitProcedure>()
            .HasOne(vp => vp.Visit)
            .WithMany(v => v.VisitProcedures)
            .HasForeignKey(vp => vp.VisitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VisitProcedure>()
            .HasOne(vp => vp.Procedure)
            .WithMany(p => p.VisitProcedures)
            .HasForeignKey(vp => vp.ProcedureId)
            .OnDelete(DeleteBehavior.Restrict);

        // MedicalRecord -> Patient
        modelBuilder.Entity<MedicalRecord>()
            .HasOne(m => m.Patient)
            .WithMany(p => p.MedicalRecords)
            .HasForeignKey(m => m.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        // Decimal precision
        modelBuilder.Entity<Visit>()
            .Property(v => v.TotalCost)
            .HasColumnType("decimal(10,2)");

        modelBuilder.Entity<Medication>()
            .Property(m => m.Price)
            .HasColumnType("decimal(10,2)");

        modelBuilder.Entity<VisitMedication>()
            .Property(vm => vm.UnitPrice)
            .HasColumnType("decimal(10,2)");

        modelBuilder.Entity<Procedure>()
            .Property(p => p.Cost)
            .HasColumnType("decimal(10,2)");

        modelBuilder.Entity<VisitProcedure>()
            .Property(vp => vp.UnitCost)
            .HasColumnType("decimal(10,2)");

        // US#9: wyszukiwanie po PESEL (równość / unikalność aktywnych pacjentów)
        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.Pesel)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // US#9: filtrowanie wizyt po lekarzu + sortowanie po dacie
        modelBuilder.Entity<Visit>()
            .HasIndex(v => new { v.DoctorId, v.ScheduledDate })
            .HasFilter("[IsDeleted] = 0");
    }
}