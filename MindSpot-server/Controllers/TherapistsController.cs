using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindSpot_server.DAL;
using MindSpot_server.Models;
using MindSpot_server.Services; // עבור OpenAiService

[ApiController]
[Route("api/[controller]")]
public class TherapistsController : ControllerBase
{
    private readonly OpenAiService _openAiService;
    private readonly NotificationService _notificationService;

    public TherapistsController(OpenAiService openAiService, NotificationService notificationService)
    {
        _openAiService = openAiService;
        _notificationService = notificationService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] Therapist therapist)
    {
        // הבדיקה האוטומטית של הקישוטים
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = "Validation failed", errors = errors });
        }

        try
        {
            var existing = therapist.GetByLicense();
            if (existing != null)
            {
                return BadRequest(new { success = false, message = "License number already exists in the system." });
            }

            int result = await therapist.RegisterWithNotification(_openAiService, _notificationService);

            if (result > 0)
            {
                return Ok(new { success = true, message = "Registration successful!", id = result });
            }

            return BadRequest(new { success = false, message = "Registration failed." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Server error: " + ex.Message });
        }
    }

    public class RegisterTherapistRequest
    {
        public string FullName { get; set; }
        public string Specialties { get; set; }
        public string Bio { get; set; }
        public string LicenseNumber { get; set; }
        public string Password { get; set; }
    }
}