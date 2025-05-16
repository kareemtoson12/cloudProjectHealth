using Microsoft.EntityFrameworkCore;
using HealthcareManagement.Shared.Models;

namespace EHR.API.Data
{
    public class EHRDbContext : DbContext
    {
        private readonly ILogger<EHRDbContext> _logger;

        public EHRDbContext(
            DbContextOptions<EHRDbContext> options,
            ILogger<EHRDbContext> logger)
            : base(options)
        {
            _logger = logger;
        }

        public DbSet<Ehr> EhrRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Ehr>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PatientId).IsRequired();
                entity.Property(e => e.DoctorId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Diagnosis).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Treatment).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Prescription).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.VisitDate).IsRequired();

                // Add indexes for common queries
                entity.HasIndex(e => e.PatientId);
                entity.HasIndex(e => e.DoctorId);
                entity.HasIndex(e => e.VisitDate);
            });
        }
    }
}