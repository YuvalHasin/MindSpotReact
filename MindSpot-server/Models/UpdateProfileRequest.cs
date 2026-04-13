using MindSpot_server.DAL;
using System.Net;

namespace MindSpot_server.Models
{
    public class UpdateProfileRequest
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }
}
