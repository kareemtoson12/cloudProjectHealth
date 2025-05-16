using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using HealthcareManagement.Shared.Models;
using AppointmentScheduling.API.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AppointmentScheduling.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly AppointmentDbContext _context;
        private readonly ILogger<AppointmentsController> _logger;
        private readonly IWebHostEnvironment _environment;

        public AppointmentsController(
            AppointmentDbContext context, 
            ILogger<AppointmentsController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Appointment>>> GetAppointments()
        {
            try
            {
                _logger.LogInformation("Attempting to retrieve all appointments");
                
                var appointments = await _context.Appointments
                    .OrderByDescending(a => a.AppointmentDateTime)
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} appointments", appointments.Count);
                return appointments;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while retrieving appointments: {Message}", dbEx.InnerException?.Message ?? dbEx.Message);
                return StatusCode(500, new { 
                    error = "A database error occurred while retrieving appointments",
                    details = _environment.IsDevelopment() ? dbEx.InnerException?.Message : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving appointments: {Message}", ex.Message);
                return StatusCode(500, new { 
                    error = "An unexpected error occurred while retrieving appointments",
                    details = _environment.IsDevelopment() ? ex.Message : null
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Appointment>> GetAppointment(Guid id)
        {
            try
            {
                var appointment = await _context.Appointments.FindAsync(id);
                if (appointment == null)
                {
                    return NotFound($"Appointment with ID {id} not found");
                }
                return appointment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointment {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the appointment");
            }
        }

        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<IEnumerable<Appointment>>> GetPatientAppointments(Guid patientId)
        {
            try
            {
                var appointments = await _context.Appointments
                    .Where(a => a.PatientId == patientId)
                    .OrderByDescending(a => a.AppointmentDateTime)
                    .ToListAsync();

                if (!appointments.Any())
                {
                    return NotFound($"No appointments found for patient {patientId}");
                }

                return appointments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointments for patient {PatientId}", patientId);
                return StatusCode(500, "An error occurred while retrieving patient appointments");
            }
        }

        [HttpGet("doctor/{doctorId}")]
        public async Task<ActionResult<IEnumerable<Appointment>>> GetDoctorAppointments(string doctorId)
        {
            try
            {
                var appointments = await _context.Appointments
                    .Where(a => a.DoctorId == doctorId)
                    .OrderByDescending(a => a.AppointmentDateTime)
                    .ToListAsync();

                if (!appointments.Any())
                {
                    return NotFound($"No appointments found for doctor {doctorId}");
                }

                return appointments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointments for doctor {DoctorId}", doctorId);
                return StatusCode(500, "An error occurred while retrieving doctor appointments");
            }
        }

        [HttpGet("available-slots")]
        public async Task<ActionResult<IEnumerable<DateTime>>> GetAvailableSlots(
            [FromQuery] string doctorId,
            [FromQuery] DateTime date)
        {
            try
            {
                // Get all appointments for the doctor on the specified date
                var existingAppointments = await _context.Appointments
                    .Where(a => a.DoctorId == doctorId && 
                           a.AppointmentDateTime.Date == date.Date &&
                           a.Status != "Cancelled")
                    .Select(a => a.AppointmentDateTime)
                    .ToListAsync();

                // Generate available slots (assuming 30-minute slots from 9 AM to 5 PM)
                var availableSlots = new List<DateTime>();
                var startTime = date.Date.AddHours(9); // 9 AM
                var endTime = date.Date.AddHours(17); // 5 PM

                for (var slot = startTime; slot < endTime; slot = slot.AddMinutes(30))
                {
                    if (!existingAppointments.Contains(slot))
                    {
                        availableSlots.Add(slot);
                    }
                }

                return availableSlots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available slots for doctor {DoctorId} on {Date}", doctorId, date);
                return StatusCode(500, "An error occurred while retrieving available slots");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Appointment>> CreateAppointment(Appointment appointment)
        {
            try
            {
                // Validate appointment time is not in the past
                if (appointment.AppointmentDateTime < DateTime.UtcNow)
                {
                    return BadRequest("Cannot create appointment in the past");
                }

                // Check if the slot is available
                var existingAppointment = await _context.Appointments
                    .AnyAsync(a => a.DoctorId == appointment.DoctorId &&
                                 a.AppointmentDateTime == appointment.AppointmentDateTime &&
                                 a.Status != "Cancelled");

                if (existingAppointment)
                {
                    return BadRequest("The selected time slot is not available");
                }

                appointment.Id = Guid.NewGuid();
                appointment.CreatedAt = DateTime.UtcNow;
                appointment.Status = "Scheduled";

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, appointment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating appointment");
                return StatusCode(500, "An error occurred while creating the appointment");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAppointment(Guid id, Appointment appointment)
        {
            try
            {
                if (id != appointment.Id)
                {
                    return BadRequest("Appointment ID mismatch");
                }

                var existingAppointment = await _context.Appointments.FindAsync(id);
                if (existingAppointment == null)
                {
                    return NotFound($"Appointment with ID {id} not found");
                }

                // Validate appointment time is not in the past
                if (appointment.AppointmentDateTime < DateTime.UtcNow)
                {
                    return BadRequest("Cannot update appointment to a time in the past");
                }

                // Check if the new slot is available (excluding the current appointment)
                var slotConflict = await _context.Appointments
                    .AnyAsync(a => a.DoctorId == appointment.DoctorId &&
                                 a.AppointmentDateTime == appointment.AppointmentDateTime &&
                                 a.Id != id &&
                                 a.Status != "Cancelled");

                if (slotConflict)
                {
                    return BadRequest("The selected time slot is not available");
                }

                appointment.UpdatedAt = DateTime.UtcNow;
                _context.Entry(existingAppointment).CurrentValues.SetValues(appointment);

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await AppointmentExists(id))
                {
                    return NotFound($"Appointment with ID {id} not found");
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment {Id}", id);
                return StatusCode(500, "An error occurred while updating the appointment");
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateAppointmentStatus(Guid id, [FromBody] string status)
        {
            try
            {
                var appointment = await _context.Appointments.FindAsync(id);
                if (appointment == null)
                {
                    return NotFound($"Appointment with ID {id} not found");
                }

                // Validate status
                var validStatuses = new[] { "Scheduled", "Completed", "Cancelled", "NoShow" };
                if (!validStatuses.Contains(status))
                {
                    return BadRequest($"Invalid status. Must be one of: {string.Join(", ", validStatuses)}");
                }

                appointment.Status = status;
                appointment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for appointment {Id}", id);
                return StatusCode(500, "An error occurred while updating the appointment status");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAppointment(Guid id)
        {
            try
            {
                var appointment = await _context.Appointments.FindAsync(id);
                if (appointment == null)
                {
                    return NotFound($"Appointment with ID {id} not found");
                }

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting appointment {Id}", id);
                return StatusCode(500, "An error occurred while deleting the appointment");
            }
        }

        private async Task<bool> AppointmentExists(Guid id)
        {
            return await _context.Appointments.AnyAsync(e => e.Id == id);
        }
    }
} 