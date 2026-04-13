using MindSpot_server.DAL;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using static BCrypt.Net.BCrypt;

namespace MindSpot_server.Models;

public class Admin
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Full Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 to 100 characters")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; }

    public string Password { get; set; }
    public string PasswordHash { get; set; }


    DBservices dbs = new DBservices();

    public Admin() { }

    public Admin(string email, string password)
    {
        this.Email = email;
        this.Password = password;
    }

    public Admin(int id, string fullName, string email, string passwordHash)
    {
        this.Id = id;
        this.FullName = fullName;
        this.Email = email;
        this.PasswordHash = passwordHash;
    }


    // התחברות אדמין
    public Admin LoginAdmin()
    {
        return dbs.SelectAdminByEmail(this.Email);
    }

    public int UpdateProfile(int id, string fullName, string email)
    {
        return dbs.UpdateAdminProfile(this.Id, this.FullName, this.Email);
    }

    // מתודה לשינוי סיסמה עבור אדמין
    public int ChangePassword(int adminId, string currentPass, string newPass)
    {
        // 1. ולידציה בסיסית לסיסמה החדשה
        if (string.IsNullOrWhiteSpace(newPass) || newPass.Length < 6)
            return 0;

        // 2. שליפת האדמין מהמסד כדי לבדוק את ה-Hash הקיים
        Admin currentAdmin = dbs.SelectAdminById(adminId);
        if (currentAdmin == null)
            return 0;

        // 3. אימות הסיסמה הנוכחית מול ה-Hash השמור
        if (!BCrypt.Net.BCrypt.Verify(currentPass, currentAdmin.PasswordHash))
            return -1; // סיסמה נוכחית שגויה

        // 4. יצירת Hash חדש ועדכון ב-SQL
        string hashedNewPassword = BCrypt.Net.BCrypt.HashPassword(newPass);
        return dbs.UpdateAdminPassword(adminId, hashedNewPassword);
    }

    // מתודה שמחזירה את הסטטיסטיקות של המערכת
    public Dictionary<string, int> GetDashboardSummary()
    {
        return dbs.GetSystemStats();
    }
}

public class AdminToken
{
    public int AdminId { get; set; }
    public string FcmToken { get; set; }

    DBservices dbs = new DBservices();

    public int SaveToken()
    {
        if (string.IsNullOrEmpty(FcmToken)) return -1;
        return dbs.UpdateAdminToken(this.AdminId, this.FcmToken);
    }
}