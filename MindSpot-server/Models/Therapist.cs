using MindSpot_server.DAL;
using MindSpot_server.Services;
using System.Collections.Generic;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace MindSpot_server.Models
{
    public class Therapist
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 to 100 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "License number is required")]
        [RegularExpression(@"^27-\d{4,6}$", ErrorMessage = "License format must be 27/XXXX or 27/XXXXX")]
        public string LicenseNumber { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^05\d{8}$", ErrorMessage = "Phone must be a valid Israeli mobile format (10 digits starting with 05)")]
        public string PhoneNumber { get; set; }

        [StringLength(1000, ErrorMessage = "Bio is too long")]
        public string Bio { get; set; }

        public string Specialties { get; set; }
        public bool IsActive { get; set; }
        public float[]? EmbeddingVector { get; set; }


        DBservices dbs = new DBservices();
        public Therapist() { }

        public async Task<int> RegisterWithNotification(OpenAiService openAiService, NotificationService notificationService)
        {
            // 1. יצירת הוקטור (Embedding)
            string textForEmbedding = $"{this.FullName}. Specialties: {this.Specialties}. Bio: {this.Bio}";
            this.EmbeddingVector = await openAiService.GenerateEmbeddingAsync(textForEmbedding);

            // 2. שמירה בבסיס הנתונים
            int result = this.Register();

            // 3. שליחת התראה לאדמין אם הרישום הצליח
            if (result > 0)
            {
                // שימוש ב-adminId 1 כברירת מחדל
                string adminToken = dbs.GetAdminToken(1);

                if (!string.IsNullOrEmpty(adminToken))
                {
                    await notificationService.SendPushNotification(
                        adminToken,
                     "New Therapist Registered! 🩺", 
                      $"Therapist {this.FullName} is waiting for your approval in MindSpot." 
                    );
                }
            }

            return result;
        }

        public int Register()
        {
            return dbs.RegisterTherapist(this);
        }

        public List<Therapist> GetAllTherapists()
        {
            return dbs.SelectAllTherapists();
        }

        public Therapist GetByLicense()
        {
            return dbs.SelectTherapistByLicense(this.LicenseNumber);
        }

        public List<Therapist> GetPendingTherapists()
        {
            return dbs.GetPendingTherapists();
        }

        // אישור מטפל
        public bool Approve(int id)
        {
            return dbs.ActivateTherapist(id) > 0;
        }

        // מחיקת מטפל (דחייה)
        public bool Reject(int id)
        {
            return dbs.DeleteTherapist(id) > 0;
        }
    }
}