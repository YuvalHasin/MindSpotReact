using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindSpot_server.Models;
using System.Net;
using static MindSpot_server.Models.Patient;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Patient")]
public class PatientsController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public IActionResult Register([FromBody] Patient patient)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = "Validation failed", errors = errors });
        }

        try
        {
            int result = patient.Register();

            if (result > 0)
            {
                return Ok(new { message = "Patient registered successfully!", id = result });
            }

            return BadRequest(new { message = "Registration failed. Check your details or email uniqueness." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("details")]
    public IActionResult GetDashboardSummary([FromQuery] int id)
    {
        try
        {
            // יצירת מופע של ה-BL (בהמשך כדאי להשתמש ב-Dependency Injection)
            Patient p = new Patient();
            var data = p.GetDashboardSummary(id);

            if (data == null)
                return NotFound(new { message = "Patient not found" });

            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal Server Error", error = ex.Message });
        }
    }

    [HttpPut("update-profile")]
    public IActionResult UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { message = "Invalid request data." });
            }

            Patient p = new Patient();
            int result = p.UpdateProfile(request);

            if (result > 0)
            {
                return Ok(new { message = "Profile updated successfully!" });
            }

            return BadRequest(new { message = "Update failed. Please check your details." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpPut("change-password")]
    public IActionResult ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { message = "Invalid request data." });
            }

            Patient p = new Patient();
            int result = p.ChangePassword(request);

            if (result == 1)
            {
                return Ok(new { message = "Password changed successfully!" });
            }

            if (result == -1)
            {
                return BadRequest(new { message = "Current password is incorrect." });
            }

            return BadRequest(new { message = "Password change failed. Ensure your new password meets requirements." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpGet("activity-history")]
    public IActionResult GetActivityHistory([FromQuery] int id)
    {
        try
        {
            Patient p = new Patient();
            var history = p.GetChatHistory(id);

            return Ok(history);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}