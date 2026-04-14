using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MindSpot_server.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            int? userId = null;
            string userRole = request.Role;

            try
            {
                if (userRole == "Patient")
                {
                    // שימוש ב-Email מה-Request
                    Patient p = new Patient(request.Email, request.Password);
                    var patient = p.LoginPatient();

                    if (patient != null && BCrypt.Net.BCrypt.Verify(request.Password, patient.PasswordHash))
                    {
                        userId = patient.Id; 
                    }
                }
                else if (userRole == "Admin")
                {
                    Admin a = new Admin(request.Email, request.Password);
                    var admin = a.LoginAdmin();

                    if (admin != null && BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
                    {
                        userId = admin.Id;
                    }
                }

                if (userId != null)
                {
                    // המרה ל-string רק עבור ייצור ה-Token
                    var token = GenerateJwtToken(userId.ToString(), userRole);
                    return Ok(new
                    {
                        token = token,
                        userId = userId, 
                        role = userRole
                    });
                }

                return Unauthorized(new { message = "Invalid login details. Please try again." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error: " + ex.Message });
            }
        }

        private string GenerateJwtToken(string userId, string role)
        {
            var keyStr = _configuration["Jwt:Key"];
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        [HttpPost("update-token")]
        public IActionResult UpdateToken([FromBody] AdminToken tokenData)
        {
            int result = tokenData.SaveToken();

            if (result > 0)
                return Ok(new { message = "Token updated successfully" });

            return BadRequest(new { message = "Failed to update token" });
        }

        // מחלקת עזר לקבלת הנתונים מה-React
        public class TokenRequest
        {
            public int AdminId { get; set; }
            public string Token { get; set; }
        }

        public class LoginRequest
        {
            public string? Email { get; set; }
            public string? LicenseNumber { get; set; }
            public string Password { get; set; }
            public string Role { get; set; }
        }
    }
}