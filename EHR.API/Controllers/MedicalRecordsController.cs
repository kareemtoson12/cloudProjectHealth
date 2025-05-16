using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using HealthcareManagement.Shared.Models;
using EHR.API.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace EHR.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EhrController : ControllerBase
    {
        private readonly EHRDbContext _context;
        private readonly ILogger<EhrController> _logger;
        private readonly IWebHostEnvironment _environment;

        public EhrController(
            EHRDbContext context, 
            ILogger<EhrController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ehr>>> GetEhrRecords()
        {
            try
            {
                _logger.LogInformation("Attempting to retrieve all EHR records");
                
                var records = await _context.EhrRecords
                    .OrderByDescending(e => e.VisitDate)
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} EHR records", records.Count);
                return records;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while retrieving EHR records: {Message}", dbEx.InnerException?.Message ?? dbEx.Message);
                return StatusCode(500, new { 
                    error = "A database error occurred while retrieving EHR records",
                    details = _environment.IsDevelopment() ? dbEx.InnerException?.Message : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving EHR records: {Message}", ex.Message);
                return StatusCode(500, new { 
                    error = "An unexpected error occurred while retrieving EHR records",
                    details = _environment.IsDevelopment() ? ex.Message : null
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Ehr>> GetEhrRecord(Guid id)
        {
            try
            {
                var record = await _context.EhrRecords.FindAsync(id);
                if (record == null)
                {
                    return NotFound($"EHR record with ID {id} not found");
                }
                return record;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving EHR record {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the EHR record");
            }
        }

        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<IEnumerable<Ehr>>> GetPatientEhrRecords(Guid patientId)
        {
            try
            {
                var records = await _context.EhrRecords
                    .Where(e => e.PatientId == patientId)
                    .OrderByDescending(e => e.VisitDate)
                    .ToListAsync();

                if (!records.Any())
                {
                    return NotFound($"No EHR records found for patient {patientId}");
                }

                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving EHR records for patient {PatientId}", patientId);
                return StatusCode(500, "An error occurred while retrieving patient EHR records");
            }
        }

        [HttpGet("doctor/{doctorId}")]
        public async Task<ActionResult<IEnumerable<Ehr>>> GetDoctorEhrRecords(string doctorId)
        {
            try
            {
                var records = await _context.EhrRecords
                    .Where(e => e.DoctorId == doctorId)
                    .OrderByDescending(e => e.VisitDate)
                    .ToListAsync();

                if (!records.Any())
                {
                    return NotFound($"No EHR records found for doctor {doctorId}");
                }

                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving EHR records for doctor {DoctorId}", doctorId);
                return StatusCode(500, "An error occurred while retrieving doctor EHR records");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Ehr>> CreateEhrRecord(Ehr record)
        {
            try
            {
                // Validate visit date is not in the future
                if (record.VisitDate > DateTime.UtcNow)
                {
                    return BadRequest("Cannot create EHR record with future date");
                }

                record.Id = Guid.NewGuid();
                record.CreatedAt = DateTime.UtcNow;
                record.Status = "Active";

                _context.EhrRecords.Add(record);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetEhrRecord), new { id = record.Id }, record);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating EHR record");
                return StatusCode(500, "An error occurred while creating the EHR record");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEhrRecord(Guid id, Ehr record)
        {
            try
            {
                if (id != record.Id)
                {
                    return BadRequest("EHR record ID mismatch");
                }

                var existingRecord = await _context.EhrRecords.FindAsync(id);
                if (existingRecord == null)
                {
                    return NotFound($"EHR record with ID {id} not found");
                }

                // Validate visit date is not in the future
                if (record.VisitDate > DateTime.UtcNow)
                {
                    return BadRequest("Cannot update EHR record to a future date");
                }

                record.UpdatedAt = DateTime.UtcNow;
                _context.Entry(existingRecord).CurrentValues.SetValues(record);

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await EhrRecordExists(id))
                {
                    return NotFound($"EHR record with ID {id} not found");
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating EHR record {Id}", id);
                return StatusCode(500, "An error occurred while updating the EHR record");
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateEhrRecordStatus(Guid id, [FromBody] string status)
        {
            try
            {
                var record = await _context.EhrRecords.FindAsync(id);
                if (record == null)
                {
                    return NotFound($"EHR record with ID {id} not found");
                }

                // Validate status
                var validStatuses = new[] { "Active", "Archived", "Deleted" };
                if (!validStatuses.Contains(status))
                {
                    return BadRequest($"Invalid status. Must be one of: {string.Join(", ", validStatuses)}");
                }

                record.Status = status;
                record.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for EHR record {Id}", id);
                return StatusCode(500, "An error occurred while updating the EHR record status");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEhrRecord(Guid id)
        {
            try
            {
                var record = await _context.EhrRecords.FindAsync(id);
                if (record == null)
                {
                    return NotFound($"EHR record with ID {id} not found");
                }

                _context.EhrRecords.Remove(record);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting EHR record {Id}", id);
                return StatusCode(500, "An error occurred while deleting the EHR record");
            }
        }

        private async Task<bool> EhrRecordExists(Guid id)
        {
            return await _context.EhrRecords.AnyAsync(e => e.Id == id);
        }
    }
}