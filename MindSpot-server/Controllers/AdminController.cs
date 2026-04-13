using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindSpot_server.DAL;
using MindSpot_server.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        // 1. קבלת כל המטפלים לטבלת הניהול
        [HttpGet("therapists")]
        public IActionResult GetAllTherapists()
        {
            try
            {
                Therapist t = new Therapist();
                var therapists = t.GetAllTherapists();
                return Ok(therapists);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving therapists list", error = ex.Message });
            }
        }

        // 2. קבלת כל המטופלים לטבלת הניהול
        [HttpGet("patients")]
        public IActionResult GetAllPatients()
        {
            try
            {
                Patient p = new Patient();
                var patients = p.GetAllPatients();
                return Ok(patients);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving patients list", error = ex.Message });
            }
        }

        // 3. קבלת סטטיסטיקות כלליות ל-Dashboard
        [HttpGet("summary")]
        public IActionResult GetPlatformSummary()
        {
            try
            {
                Admin adminLogic = new Admin();
                var stats = adminLogic.GetDashboardSummary();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                // החזרת שגיאה בפורמט JSON אחיד
                return StatusCode(500, new { message = "Error loading dashboard summary", details = ex.Message });
            }
        }

        // 2. מחיקת מטופל
        [HttpDelete("delete-patient/{id}")]
        public IActionResult DeletePatient(int id)
        {
            try
            {
                Patient p = new Patient();
                int result = p.DeletePatient(id);

                if (result > 0)
                    return Ok(new { message = "Patient deleted successfully" });

                return NotFound(new { message = "Patient not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("update-profile-full")]
        public IActionResult UpdateFullProfile([FromBody] AdminUpdateRequest request)
        {
            // בדיקת הקישוטים (Name, Email)
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = "Validation failed", errors = errors });
            }

            try
            {
                Admin admin = new Admin();

                // עדכון פרטים בסיסיים
                int profileResult = admin.UpdateProfile(request.Id, request.FullName, request.Email);

                // שינוי סיסמה אם נדרש
                if (!string.IsNullOrEmpty(request.NewPassword))
                {
                    int passResult = admin.ChangePassword(request.Id, request.CurrentPassword, request.NewPassword);
                    if (passResult == -1) return BadRequest(new { message = "The current password is incorrect" });
                    if (passResult == 0) return BadRequest(new { message = "The new password is too short" });
                }

                return Ok(new { success = true, message = "Profile updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error updating admin profile", details = ex.Message });
            }
        }

        [HttpGet("therapists/pending")] 
        public IActionResult GetPending()
        {
            try
            {
                Therapist t = new Therapist();
                List<Therapist> pendingList = t.GetPendingTherapists();

                return Ok(pendingList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("therapists/{id}/approve")]
        public IActionResult ApproveTherapist(int id)
        {
            try
            {
                Therapist t = new Therapist();
                bool success = t.Approve(id); 

                if (success)
                {
                    return Ok(new { message = $"Therapist with ID {id} has been approved." });
                }
                return NotFound(new { message = "Therapist not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error approving therapist: " + ex.Message });
            }
        }

        [HttpDelete("therapists/{id}/reject")]
        public IActionResult RejectTherapist(int id)
        {
            try
            {
                Therapist t = new Therapist();
                if (t.Reject(id))
                    return Ok(new { message = "Therapist request rejected and deleted." });

                return NotFound(new { message = "Therapist not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public class AdminUpdateRequest
        {
            public int Id { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string CurrentPassword { get; set; }
            public string NewPassword { get; set; }
        }
    }
}