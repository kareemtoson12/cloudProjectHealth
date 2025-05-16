// PatientDbContext.cs
using Microsoft.EntityFrameworkCore;
using HealthcareManagement.Shared.Models;

namespace PatientManagement.API.Data
{
    public class PatientDbContext : DbContext
    {
        public PatientDbContext(DbContextOptions<PatientDbContext> options)
            : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DateOfBirth).IsRequired();
                entity.Property(e => e.Gender).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(200);
                entity.Property(e => e.EmergencyContact).IsRequired().HasMaxLength(100);
                entity.Property(e => e.InsuranceProvider).HasMaxLength(100);
                entity.Property(e => e.InsurancePolicyNumber).HasMaxLength(50);
                entity.Property(e => e.RegistrationDate).IsRequired();

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => new { e.LastName, e.FirstName });
            });
        }
    }
}

// PatientDbContextModelSnapshot.cs
// This would be auto-generated when you create the migration