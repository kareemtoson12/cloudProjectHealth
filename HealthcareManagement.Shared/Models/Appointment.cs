using System;

namespace HealthcareManagement.Shared.Models
{
    public class Appointment
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string DoctorId { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public string AppointmentType { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public int DurationMinutes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
} 