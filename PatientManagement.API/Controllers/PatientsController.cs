// PatientsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthcareManagement.Shared.Models;
using PatientManagement.API.Data;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace PatientManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly PatientDbContext _context;
        private readonly ILogger<PatientsController> _logger;
        private readonly IWebHostEnvironment _environment;

        public PatientsController(
            PatientDbContext context, 
            ILogger<PatientsController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Patient>>> GetPatients()
        {
            try
            {
                _logger.LogInformation("Attempting to retrieve all patients");
                var patients = await _context.Patients
                    .OrderBy(p => p.LastName)
                    .ThenBy(p => p.FirstName)
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} patients", patients.Count);
                return patients;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while retrieving patients: {Message}", dbEx.InnerException?.Message ?? dbEx.Message);
                return StatusCode(500, new { 
                    error = "A database error occurred while retrieving patients",
                    details = _environment.IsDevelopment() ? dbEx.InnerException?.Message : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving patients: {Message}", ex.Message);
                return StatusCode(500, new { 
                    error = "An unexpected error occurred while retrieving patients",
                    details = _environment.IsDevelopment() ? ex.Message : null
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Patient>> GetPatient(Guid id)
        {
            try
            {
                var patient = await _context.Patients.FindAsync(id);
                if (patient == null)
                {
                    return NotFound($"Patient with ID {id} not found");
                }
                return patient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the patient");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Patient>>> SearchPatients([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest("Search term cannot be empty");
                }

                var patients = await _context.Patients
                    .Where(p => 
                        p.FirstName.Contains(searchTerm) || 
                        p.LastName.Contains(searchTerm) ||
                        p.Email.Contains(searchTerm) ||
                        p.PhoneNumber.Contains(searchTerm))
                    .OrderBy(p => p.LastName)
                    .ThenBy(p => p.FirstName)
                    .ToListAsync();

                return patients;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching patients with term {SearchTerm}", searchTerm);
                return StatusCode(500, "An error occurred while searching patients");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Patient>> CreatePatient(Patient patient)
        {
            try
            {
                // Validate email uniqueness
                if (await _context.Patients.AnyAsync(p => p.Email == patient.Email))
                {
                    return BadRequest("A patient with this email already exists");
                }

                patient.Id = Guid.NewGuid();
                patient.RegistrationDate = DateTime.UtcNow;

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPatient), new { id = patient.Id }, patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient");
                return StatusCode(500, "An error occurred while creating the patient");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(Guid id, Patient patient)
        {
            try
            {
                if (id != patient.Id)
                {
                    return BadRequest("Patient ID mismatch");
                }

                var existingPatient = await _context.Patients.FindAsync(id);
                if (existingPatient == null)
                {
                    return NotFound($"Patient with ID {id} not found");
                }

                // Validate email uniqueness (excluding current patient)
                if (await _context.Patients.AnyAsync(p => p.Email == patient.Email && p.Id != id))
                {
                    return BadRequest("A patient with this email already exists");
                }

                _context.Entry(existingPatient).CurrentValues.SetValues(patient);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await PatientExists(id))
                {
                    return NotFound($"Patient with ID {id} not found");
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient {Id}", id);
                return StatusCode(500, "An error occurred while updating the patient");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(Guid id)
        {
            try
            {
                var patient = await _context.Patients.FindAsync(id);
                if (patient == null)
                {
                    return NotFound($"Patient with ID {id} not found");
                }

                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient {Id}", id);
                return StatusCode(500, "An error occurred while deleting the patient");
            }
        }

        private async Task<bool> PatientExists(Guid id)
        {
            return await _context.Patients.AnyAsync(e => e.Id == id);
        }
    }
}