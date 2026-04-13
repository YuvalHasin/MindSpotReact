using MindSpot_server.DAL;
using System.Net;

namespace MindSpot_server.Models
{
    public class ChangePasswordRequest
    {
        public int Id { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
