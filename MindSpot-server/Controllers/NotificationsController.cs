using Microsoft.AspNetCore.Mvc;
using MindSpot_server.DAL;
using MindSpot_server.Models;
using System.Net;

[Route("api/[controller]")]
[ApiController]
public class NotificationsController : ControllerBase
{
    // שמירת טוקן של אדמין
    [HttpPost("save-token")]
    public IActionResult SaveToken([FromBody] AdminToken tokenData)
    {
        if (tokenData == null) return BadRequest("Invalid data");

        try
        {
            int result = tokenData.SaveToken();
            if (result > 0)
            {
                return Ok(new { message = "Token saved/updated successfully" });
            }
            return BadRequest("Failed to save token");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}

