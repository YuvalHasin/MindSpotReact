using MindSpot_server.Models;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace MindSpot_server.DAL
{
    public class DBservices
    {
        public DBservices() { }

        public SqlConnection connect(string conString)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json").Build();
            string cStr = configuration.GetConnectionString("MindSpotDB");
            SqlConnection con = new SqlConnection(cStr);
            con.Open();
            return con;
        }

        // פונקציית עזר גנרית ליצירת Command
        private SqlCommand CreateCommandWithStoredProcedureGeneral(string spName, SqlConnection con, Dictionary<string, object> paramDic)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = spName;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 10;

            if (paramDic != null)
            {
                foreach (KeyValuePair<string, object> param in paramDic)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }
            return cmd;
        }

        // ================= PATIENTS =================

        // רישום מטופל חדש
        public int RegisterPatient(Patient p)
        {
            SqlConnection con = connect("MindSpotDB");
            Dictionary<string, object> paramDic = new Dictionary<string, object>
            {
                { "@FullName", p.FullName },
                { "@Email", p.Email },
                { "@PasswordHash", p.PasswordHash }
            };

            SqlCommand cmd = CreateCommandWithStoredProcedureGeneral("sp_RegisterPatient", con, paramDic);
            try
            {
                return Convert.ToInt32(cmd.ExecuteScalar()); // מחזיר את ה-ID החדש
            }
            finally { con.Close(); }
        }

        public object GetPatientDashboardData(int id)
        {
            SqlConnection con = connect("MindSpotDB");
            Dictionary<string, object> paramDic = new Dictionary<string, object>
    {
        { "@Id", id }
    };

            // 1. יצירת ה-Command
            SqlCommand cmd = CreateCommandWithStoredProcedureGeneral("sp_GetPatientDashboardSummary", con, paramDic);

            try
            {
                if (con.State == ConnectionState.Closed) con.Open();

                SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        return new
                        {
                            fullName = reader["FullName"] != DBNull.Value ? reader["FullName"].ToString() : "",
                            email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : "",
                            totalSessions = reader["TotalSessions"] != DBNull.Value ? Convert.ToInt32(reader["TotalSessions"]) : 0,
                            sessionsThisMonth = reader["SessionsThisMonth"] != DBNull.Value ? Convert.ToInt32(reader["SessionsThisMonth"]) : 0
                        };
                    }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Dashboard Error: " + ex.Message);
            }
            finally
            {
                con.Close();

            }
        }

        // שליפת כל המטופלים
        public List<Patient> GetAllPatients()
        {
            SqlConnection con = connect("MindSpotDB");
            SqlCommand cmd = CreateCommandWithStoredProcedureGeneral("sp_GetAllPatients", con, null);
            List<Patient> list = new List<Patient>();

            try
            {
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(new Patient
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        FullName = dr["FullName"].ToString(),
                        Email = dr["Email"].ToString(),
                        LastTriageDate = dr["LastTriageDate"] != DBNull.Value ? Convert.ToDateTime(dr["LastTriageDate"]) : null,
                    });
                }
                return list;
            }
            finally { con.Close(); }
        }

        public List<Dictionary<string, object>> GetPatientChatHistory(int patientId)
        {
            SqlConnection con = null;
            List<Dictionary<string, object>> history = new List<Dictionary<string, object>>();

            try
            {
                con = connect("MindSpotDB");
                Dictionary<string, object> paramDic = new Dictionary<string, object>();
                paramDic.Add("@PatientId", patientId);

                SqlCommand cmd = CreateCommandWithStoredProcedureGeneral("sp_GetPatientChatHistory", con, paramDic);

                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Dictionary<string, object> session = new Dictionary<string, object>();
                    session["Id"] = dr["Id"];
                    session["CreatedAt"] = dr["CreatedAt"].ToString();
                    session["MessageCount"] = dr["MessageCount"];
                    session["Summary"] = dr["Summary"].ToString();
                    session["TherapistName"] = dr["TherapistName"].ToString();

                    history.Add(session);
                }
                return history;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (con != null) con.Close();
            }
        }

        public int UpdatePatientTriage(int patientId, string summary, string embeddingVector)
        {
            SqlConnection con = null;
            try
            {
                con = connect("MindSpotDB");
                Dictionary<string, object> paramDic = new Dictionary<string, object>
        {
            { "@Id", patientId },
            { "@Summary", summary },
            { "@Vector", embeddingVector },
            { "@Date", DateTime.UtcNow }
        };

                // ודאי שיצרת Stored Procedure בשם sp_UpdatePatientTriage
                SqlCommand cmd = CreateCommandWithStoredProcedureGeneral("sp_UpdatePatientTriage", con, paramDic);

                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (con != null) con.Close();
            }
        }

        public int UpdatePatient(Patient p)
        {
            SqlConnection con = null;
            try
            {
                // 1. יצירת חיבור חדש
                con = connect("MindSpotDB");

                Dictionary<string, object> paramDic = new Dictionary<string, object>();
                paramDic.Add("@Id", p.Id);
                paramDic.Add("@FullName", p.FullName);
                paramDic.Add("@Email", p.Email);

                SqlCommand cmd = CreateCommandWithStoredProcedureGeneral("sp_UpdatePatient", con, paramDic);

                // 2. הגנה קריטית: פותחים רק אם סגור
                if (con.State == ConnectionState.Closed)
                {
                    con.Open();
                }

                // 3. שימוש ב-ExecuteScalar כי ה-SP מחזירה SELECT @@ROWCOUNT
                object result = cmd.ExecuteScalar();

                // 4. החזרת הערך (אמור להיות 1)
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex)
            {
                // אם ה-RAISERROR מה-SQL קופץ, הוא יגיע לכאן
                throw new Exception(ex.Message);
            }
            finally
            {
                con.Close();
            }
        }

        public Patient SelectPatientById(int userId)
        {
            SqlConnection con = connect("MindSpotDB");
            SqlCommand cmd = new SqlCommand("sp_GetPatientById", con);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Id", userId);

            SqlDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                return new Patient
                {
                    Id = Convert.ToInt32(rdr["Id"]),
                    FullName = rdr["FullName"].ToString(),
                    Email = rdr["Email"].ToString(),
                    PasswordHash = rdr["PasswordHash"].ToString(),
                    LastTriageSummary = rdr["LastTriageSummary"]?.ToString(),
                    LastTriageDate = rdr["LastTriageDate"] != DBNull.Value ?
                                     Convert.ToDateTime(rdr["LastTriageDate"]) : (DateTime?)null
                };
            }
            return null;
        }

        public Patient SelectPatientByEmail(string email)
        {
            SqlConnection con = null;
            try
            {
                con = connect("MindSpotDB");
                Dictionary<string, object> paramDic = new Dictionary<string, object>
        {
            { "@Email", email }
        };

                // וודאי שיש לך Stored Procedure בשם sp_GetPatientByEmail
                SqlCommand cmd = CreateCommandWithStoredProcedureGeneral("sp_GetPatientByEmail", con, paramDic);

                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    return new Patient
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        FullName = dr["FullName"].ToString(),
                        Email = dr["Email"].ToString(),
                        PasswordHash = dr["PasswordHash"].ToString(),
                        // שדות נוספים אם את צריכה אותם בזמן ה-Login
                        LastTriageSummary = dr["LastTriageSummary"]?.ToString()
                    };
                }
                return null; // לא נמצא מטופל עם האימייל הזה
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (con != null) con.Close();
            }
        }

        public int UpdatePatientPassword(int userId, string hashedNewPassword)
        {
            SqlConnection con = null;
            try
            {
                con = connect("MindSpotDB");
                Dictionary<string, object> paramDic = new Dictionary<string, object>
        {
            { "@UserId", userId },
            { "@NewPassword", hashedNewPassword }
        };

                SqlCommand cmd = CreateCommandWithStoredProcedureGeneral("sp_UpdatePatientPassword", con, paramDic);

                // חייב לפתוח את החיבור לפני הביצוע
                if (con.State == ConnectionState.Closed)
                    con.Open();

                // שימוש ב-Scalar כדי לקבל את ה-1 שמוחזר מה-SELECT @@ROWCOUNT
                object result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating password: " + ex.Message);
            }
            finally
            {
                con.Close();
            }
        }
        // ================= THERAPISTS =================

        // רישום מטפל
        public int RegisterTherapist(Therapist therapist)
        {
            SqlConnection con = connect("MindSpotDB");

            if (con.State == System.Data.ConnectionState.Open)
            {
                con.Close();
            }

            // הכנת המילון עם הנתונים
            Dictionary<string, object> parameters = new Dictionary<string, object>
    {
        { "@FullName", therapist.FullName },
        { "@LicenseNumber", therapist.LicenseNumber },
        { "@Bio", (object)therapist.Bio ?? DBNull.Value },
        { "@Specialties", (object)therapist.Specialties ?? DBNull.Value },
        { "@PhoneNumber", (object)therapist.PhoneNumber ?? DBNull.Value },
        { "@Vector", (object)System.Text.Json.JsonSerializer.Serialize(therapist.EmbeddingVector) ?? DBNull.Value }
    };

            SqlCommand cmd = new SqlCommand("sp_RegisterTherapist", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            // הוספת הפרמטרים מהדיקשנרי בצורה דינמית
            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value);
            }

            try
            {
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                throw new Exception("Error in RegisterTherapist with Dictionary: " + ex.Message);
            }
            finally
            {
                con.Close();
            }
        }

        public Therapist SelectTherapistByLicense(string licenseNumber)
        {
            SqlConnection con = null;
            try
            {
                con = connect("MindSpotDB");
                Dictionary<string, object> paramDic = new Dictionary<string, object>
        {
            { "@LicenseNumber", licenseNumber }
        };

                // ודאי שיש לך Stored Procedure בשם sp_GetTherapistByLicense
                SqlCommand cmd = CreateCommandWithStoredProcedureGeneral("sp_GetTherapistByLicense", con, paramDic);

                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    return new Therapist
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        FullName = dr["FullName"].ToString(),
                        LicenseNumber = dr["LicenseNumber"].ToString(),
                        Specialties = dr["Specialties"]?.ToString(),
                        Bio = dr["Bio"]?.ToString(),
                        PhoneNumber = dr["PhoneNumber"] != DBNull.Value ? dr["PhoneNumber"].ToString() : ""
                    };
                }
                return null; // לא נמצא מטפל עם מספר רישיון כזה
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (con != null) con.Close();
            }
        }

        // 1. שליפת כל המטפלים (לצורך תצוגה או חישוב התאמה)
        public List<Therapist> SelectAllTherapists()
        {
            List<Therapist> therapists = new List<Therapist>();
            SqlConnection con = connect("MindSpotDB");
            SqlCommand cmd = new SqlCommand("SELECT * FROM Therapists", con);
            cmd.CommandType = CommandType.Text;
            SqlDataReader rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                therapists.Add(new Therapist
                {
                    Id = Convert.ToInt32(rdr["Id"]),
                    FullName = rdr["FullName"].ToString(),
                    LicenseNumber = rdr["LicenseNumber"].ToString(),
                    Bio = rdr["Bio"].ToString(),
                    Specialties = rdr["Specialties"].ToString(),
                    PhoneNumber = rdr["PhoneNumber"] != DBNull.Value ? rdr["PhoneNumber"].ToString() : "",
                    IsActive = rdr["IsActive"] != DBNull.Value && Convert.ToBoolean(rdr["IsActive"])
                });
            }
            return therapists;
        }

        // ================= CHAT & TRIAGE =================

        // יצירת סשן חדש
        public int CreateChatSession(int patientId)
        {
            SqlConnection con = connect("MindSpotDB");
            Dictionary<string, object> paramDic = new Dictionary<string, object> { { "@PatientId", patientId } };
            SqlCommand cmd = CreateCommandWithStoredProcedureGeneral("sp_CreateChatSession", con, paramDic);
            try { return Convert.ToInt32(cmd.ExecuteScalar()); }
            finally { con.Close(); }
        }

        // עדכון תוצאות אבחון (Triage)
        public int UpdateTriageResults(int patientId, int sessionId, string summary, float[] embedding, string therapistName, int? therapistId)
        {
            SqlConnection con = connect("MindSpotDB");
            Dictionary<string, object> paramDic = new Dictionary<string, object>
            {
                { "@PatientId", patientId },
                { "@SessionId", sessionId },
                { "@Summary", summary },
                { "@EmbeddingJson", Newtonsoft.Json.JsonConvert.SerializeObject(embedding) },
                { "@RecommendedTherapistId", (object)therapistId ?? DBNull.Value },
                { "@TherapistName", (object)therapistName ?? DBNull.Value }
            };

            SqlCommand cmd = CreateCommandWithStoredProcedureGeneral("sp_UpdateTriageResults", con, paramDic);

            try
            {
                if (con.State == System.Data.ConnectionState.Closed) con.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in UpdateTriageResults: " + ex.Message);
                throw;
            }
            finally { con.Close(); }
        }

        public int InsertChatSession(int patientId, string summary, string therapistName)
        {
            SqlConnection con = connect("MindSpotDB");

            // בדיקה ומניעת שגיאת חיבור פתוח - ניהול ידני כפי שביקשת
            if (con.State == System.Data.ConnectionState.Open)
            {
                con.Close();
            }

            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "@PatientId", patientId },
                { "@Summary", (object)summary ?? DBNull.Value },
                { "@TherapistName", (object)therapistName ?? DBNull.Value }
            };

            SqlCommand cmd = new SqlCommand("sp_CreateChatSession", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value);
            }

            try
            {
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                throw new Exception("Error in InsertChatSession with Dictionary: " + ex.Message);
            }
            finally
            {
                con.Close();
            }
        }

        public List<ChatSession> SelectChatSessionsByPatient(int patientId)
        {
            SqlConnection con = null;
            List<ChatSession> sessions = new List<ChatSession>();

            try
            {
                con = connect("MindSpotDB");
                Dictionary<string, object> paramDic = new Dictionary<string, object>
        {
            { "@PatientId", patientId }
        };

                // קריאה ל-Stored Procedure ששולף היסטוריה
                SqlCommand cmd = CreateCommandWithStoredProcedureGeneral("sp_GetPatientChatHistory", con, paramDic);

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    ChatSession session = new ChatSession();
                    session.Id = Convert.ToInt32(dr["Id"]);
                    session.CreatedAt = dr["CreatedAt"].ToString();
                    session.Summary = dr["Summary"].ToString();

                    // השורה המעודכנת שקוראת את השם מה-SQL
                    session.TherapistName = dr["TherapistName"] != DBNull.Value ?
                                            dr["TherapistName"].ToString() : "Pending Match";

                    sessions.Add(session);
                }
                return sessions;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (con != null) con.Close();
            }
        }

        // ================= ADMIN & SYSTEM =================

        // שליפת סטטיסטיקות דשבורד
        public Dictionary<string, int> GetSystemStats()
        {
            SqlConnection con = null;
            Dictionary<string, int> stats = new Dictionary<string, int>();

            try
            {
                con = connect("MindSpotDB");
                // ודאי שהפרמטר השלישי הוא null כי אין פרמטרים ל-SP הזה
                SqlCommand cmd = CreateCommandWithStoredProcedureGeneral("sp_GetSystemStats", con, null);

                // בדיקה למניעת שגיאת "Connection was not closed"
                if (con.State == ConnectionState.Open) con.Close();

                con.Open(); // חובה לפתוח לפני ה-Execute!

                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    // שימוש באותיות קטנות במפתחות כדי להתאים ל-JS/React
                    stats.Add("totalPatients", Convert.ToInt32(dr["TotalPatients"]));
                    stats.Add("totalTherapists", Convert.ToInt32(dr["TotalTherapists"]));
                    stats.Add("pendingTherapists", Convert.ToInt32(dr["PendingTherapists"]));
                }
                return stats;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching dashboard stats: " + ex.Message);
            }
            finally
            {
                if (con != null && con.State != ConnectionState.Closed)
                    con.Close();
            }
        }

        public Admin SelectAdminByEmail(string email)
        {
            SqlConnection con = null;
            con = connect("MindSpotDB");
            Dictionary<string, object> paramDic = new Dictionary<string, object> { { "@Email", email } };
            SqlCommand cmd = CreateCommandWithStoredProcedureGeneral("sp_GetAdminByEmail", con, paramDic);

            try
            {
                SqlDataReader dr = cmd.ExecuteReader();
                {
                    if (dr.Read())
                    {
                        return new Admin(
                            Convert.ToInt32(dr["Id"]),
                            dr["FullName"].ToString(),
                            dr["Email"].ToString(),
                            dr["PasswordHash"].ToString()
                        );
                    }
                }
                return null;
            }
            finally
            {
                // זה החלק שמונע את השגיאה שקיבלת - סגירה מוחלטת של החיבור
                if (con != null && con.State != ConnectionState.Closed)
                {
                    con.Close();
                }
            }
        }

        // שליפת אדמין לפי ID
        public Admin SelectAdminById(int id)
        {
            SqlConnection con = null;
            try
            {
                con = connect("MindSpotDB");
                Dictionary<string, object> paramDic = new Dictionary<string, object> { { "@Id", id } };
                SqlCommand cmd = CreateCommandWithStoredProcedureGeneral("sp_GetAdminById", con, paramDic);
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    return new Admin(
                        Convert.ToInt32(dr["Id"]),
                        dr["FullName"].ToString(),
                        dr["Email"].ToString(),
                        dr["PasswordHash"].ToString()
                    );
                }
                return null;
            }
            finally { if (con != null) con.Close(); }
        }

        public int UpdateAdminProfile(int id, string fullName, string email)
        {
            SqlConnection con = connect("MindSpotDB");

            // בדיקת מצב החיבור לפני תחילת העבודה
            if (con.State == System.Data.ConnectionState.Open)
            {
                con.Close();
            }

            Dictionary<string, object> parameters = new Dictionary<string, object>
    {
        { "@Id", id },
        { "@FullName", fullName },
        { "@Email", email }
    };

            // הגדרת הפקודה לעבודה מול ה-SP שצירפת
            SqlCommand cmd = new SqlCommand("sp_UpdateAdminProfile", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            // הוספת הפרמטרים באופן ידני מתוך הדיקשנרי (בלי foreach)
            cmd.Parameters.AddWithValue("@Id", parameters["@Id"]);
            cmd.Parameters.AddWithValue("@FullName", parameters["@FullName"] ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", parameters["@Email"] ?? DBNull.Value);

            try
            {
                con.Open();
                // ExecuteNonQuery מחזיר את מספר השורות שהושפעו (אמור להיות 1)
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // זריקת שגיאה מפורטת לשכבת ה-BL
                throw new Exception("Error in UpdateAdminProfile: " + ex.Message);
            }
            finally
            {
                // סגירה ידנית של החיבור - קריטי להרצה בשרת רופין לפי ההנחיות
                if (con != null)
                {
                    con.Close();
                }
            }
        }

        // עדכון סיסמת אדמין בבסיס הנתונים
        public int UpdateAdminPassword(int adminId, string hashedNewPassword)
        {
            SqlConnection con = null;
            try
            {
                con = connect("MindSpotDB"); // התחברות למסד הנתונים שלך

                Dictionary<string, object> paramDic = new Dictionary<string, object>
        {
            { "@Id", adminId },
            { "@NewPassword", hashedNewPassword }
        };

                // קריאה ל-Stored Procedure שמעדכן סיסמה
                // ודאי שיצרת ב-SQL פרוצדורה בשם sp_UpdateAdminPassword
                SqlCommand cmd = CreateCommandWithStoredProcedureGeneral("sp_UpdateAdminPassword", con, paramDic);

                return cmd.ExecuteNonQuery(); // מחזיר 1 אם העדכון הצליח
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (con != null)
                {
                    con.Close();
                }
            }
        }

        public int DeletePatient(int id)
        {
            SqlConnection con = null;
            try
            {
                con = connect("MindSpotDB");
                Dictionary<string, object> paramDic = new Dictionary<string, object>
        {
            { "@PatientId", id }
        };

                SqlCommand cmd = CreateCommandWithStoredProcedureGeneral("sp_DeletePatient", con, paramDic);

                if (con.State == ConnectionState.Open) con.Close();

                con.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting patient: " + ex.Message);
            }
            finally
            {
                if (con != null && con.State != ConnectionState.Closed) con.Close();
            }
        }

        public List<Therapist> GetPendingTherapists()
        {
            // שאילתה פשוטה שמחזירה רק את אלו שלא פעילים
            return SelectAllTherapists().Where(t => !t.IsActive).ToList();
        }

        public int UpdateAdminToken(int adminId, string token)
        {
            SqlConnection con = connect("MindSpotDB");

            if (con.State == System.Data.ConnectionState.Open)
            {
                con.Close();
            }

            SqlCommand cmd = new SqlCommand("sp_UpdateAdminToken", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@adminId", adminId);
            cmd.Parameters.AddWithValue("@token", token);

            try
            {
                con.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error in UpdateAdminToken: " + ex.Message);
            }
            finally
            {
                con.Close(); 
            }
        }

        public string GetAdminToken(int adminId)
        {
            SqlConnection con = connect("MindSpotDB");
            if (con.State == System.Data.ConnectionState.Open)
            {
                con.Close();
            }

            SqlCommand cmd = new SqlCommand("sp_GetAdminToken", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            // הוספת הפרמטר
            cmd.Parameters.AddWithValue("@adminId", adminId);

            try
            {
                con.Open();
                object result = cmd.ExecuteScalar();

                return result != null ? result.ToString() : null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in GetAdminToken (SP): " + ex.Message);
            }
            finally
            {
                con.Close();
            }
        }

        // 1. אישור מטפל (שינוי ל-Active)
        public int ActivateTherapist(int id)
        {
            SqlConnection con = null;
            try
            {
                con = connect("MindSpotDB");
                // אפשר להשתמש בשאילתה ישירה או ב-SP
                string query = "UPDATE Therapists SET isActive = 1 WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);
                return cmd.ExecuteNonQuery();
            }
            finally { if (con != null) con.Close(); }
        }

        // 2. מחיקת מטפל (דחייה)
        public int DeleteTherapist(int id)
        {
            SqlConnection con = null;
            try
            {
                con = connect("MindSpotDB");
                string query = "DELETE FROM Therapists WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);
                return cmd.ExecuteNonQuery();
            }
            finally { if (con != null) con.Close(); }
        }
    }
}