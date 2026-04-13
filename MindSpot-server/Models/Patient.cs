using MindSpot_server.DAL;
using System.Data;
using System.Text.RegularExpressions;
using static BCrypt.Net.BCrypt;
using System.ComponentModel.DataAnnotations;

namespace MindSpot_server.Models;

public class Patient
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 to 100 characters")]
    [RegularExpression(@"^[A-Za-z\u0590-\u05FF\s]+$", ErrorMessage = "Name can only contain letters and spaces")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(20, MinimumLength = 6, ErrorMessage = "Password must be between 6 to 20 characters")]
    // אפשר להוסיף כאן גם דרישה לספרה או אות גדולה אם תרצי
    public string Password { get; set; }

    public string? PasswordHash { get; set; }
    public string? LastTriageSummary { get; set; }
    public float[]? TriageEmbedding { get; set; }
    public DateTime? LastTriageDate { get; set; }

    DBservices dbs = new DBservices();

    public Patient() { }

    public Patient(string email, string password)
    {
        this.Email = email;
        this.Password = password;
    }


    public int Register()
    {
        this.PasswordHash = HashPassword(this.Password);
        return dbs.RegisterPatient(this);
    }

    public List<Patient> GetAllPatients()
    {
        return dbs.GetAllPatients();
    }

    // עדכון תוצאות אבחון
    public int UpdateTriage(int sessionId, string summary, float[] embedding, string TherapistName, int therapistId)
    {
        return dbs.UpdateTriageResults(this.Id, sessionId, summary, embedding, TherapistName, therapistId);
    }

    // מחזיר את היסטוריית הצ'אטים של המטופל הנוכחי
    public List<Dictionary<string, object>> GetChatHistory(int patientId)
    {
        return dbs.GetPatientChatHistory(patientId);
    }

    // שליפת נתוני מטופל לפי ID
    public Patient GetUserById(int userId)
    {
        return dbs.SelectPatientById(userId);
    }

    // עדכון פרטי פרופיל (שם ואימייל)
    public int UpdateProfile(UpdateProfileRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FullName) || req.FullName.Length < 2) return 0;
        if (!req.Email.Contains("@")) return 0;

        Patient pToUpdate = new Patient
        {
            Id = req.Id,
            FullName = req.FullName,
            Email = req.Email
        };

        return dbs.UpdatePatient(pToUpdate);
    }

    // שינוי סיסמה (כולל אימות סיסמה ישנה)
    public int ChangePassword(ChangePasswordRequest req)
    {
        // 1. שליפת המטופל מהדאטהבייס כדי לקבל את ה-Hash הנוכחי
        Patient currentPatient = dbs.SelectPatientById(req.Id);
        if (currentPatient == null) return 0;

        // 2. אימות הסיסמה הישנה מול ה-Hash השמור (באמצעות BCrypt)
        bool isOldPasswordCorrect = BCrypt.Net.BCrypt.Verify(req.CurrentPassword, currentPatient.PasswordHash);

        if (!isOldPasswordCorrect) return -1; // קוד שגיאה עבור סיסמה שגויה

        // 3. הצפנת הסיסמה החדשה
        string newHashedPassword = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

        return dbs.UpdatePatientPassword(req.Id, newHashedPassword);
    }

    // מתודה לחיפוש מטופל לצורך התחברות
    public Patient LoginPatient()
    {
        return dbs.SelectPatientByEmail(this.Email);
    }

    // עדכון תוצאות השאלון (Triage) ב-SQL
    public int UpdateTriageData(string summary, float[] embedding)
    {
        this.LastTriageSummary = summary;
        this.LastTriageDate = DateTime.UtcNow;

        // הפיכת הווקטור למחרוזת מופרדת בפסיקים לצורך שמירה ב-DB
        string embeddingString = string.Join(",", embedding);

        return dbs.UpdatePatientTriage(this.Id, summary, embeddingString);
    }

    public object GetDashboardSummary(int id)
    {
        return dbs.GetPatientDashboardData(id);
    }

    public int DeletePatient(int id)
    {
        return dbs.DeletePatient(id);
    }
}