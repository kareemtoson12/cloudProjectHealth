using System.ComponentModel.DataAnnotations;

namespace HealthcareManagement.Shared.Models
{
    public class Ehr
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PatientId { get; set; }

        [Required]
        public DateTime VisitDate { get; set; }

        [Required]
        public string Diagnosis { get; set; }

        [Required]
        public string Treatment { get; set; }

        [Required]
        public string Prescription { get; set; }

        public string Notes { get; set; }

        [Required]
        public string DoctorId { get; set; }

        // Attachments stored as a JSON string (or you can use a separate table if needed)
        public string Attachments { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
} 