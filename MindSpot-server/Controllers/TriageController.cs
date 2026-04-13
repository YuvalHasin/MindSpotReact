using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindSpot_server.Models;
using MindSpot_server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Patient")]
    public class TriageController : ControllerBase
    {
        private readonly OpenAiService _openAiService;

        public TriageController(OpenAiService openAiService)
        {
            _openAiService = openAiService;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitTriage([FromBody] TriageRequest request)
        {
            Patient p = new Patient();
            var patient = p.GetUserById(request.PatientId);
            if (patient == null) return NotFound("Patient not found");

            try
            {
                // 2. ניתוח OpenAI
                var summary = await _openAiService.SummarizePatientStateAsync(request.AnswersText);
                var embedding = await _openAiService.GenerateEmbeddingAsync(request.AnswersText);

                if (embedding == null || embedding.Length == 0)
                    return BadRequest("Vector generation failed.");

                // 3. עדכון נתוני המטופל בשכבת ה-BL
                patient.LastTriageSummary = summary;
                patient.LastTriageDate = DateTime.UtcNow;
                // ב-SQL אנחנו שומרים את הוקטור כטקסט/JSON בדרך כלל
                patient.UpdateTriageData(summary, embedding);

                // 4. חיפוש וקטורי (התאמת מטפלים)
                Therapist t = new Therapist();
                var allTherapists = t.GetAllTherapists();

                // חישוב התאמה וקטורית ב-C# (Cosine Similarity)
                var matchedTherapists = allTherapists
                    .Select(therapist => new {
                        Therapist = therapist,
                        Score = CalculateCosineSimilarity(embedding, therapist.EmbeddingVector)
                    })
                    .OrderByDescending(x => x.Score)
                    .Take(3)
                    .Select(x => x.Therapist)
                    .ToList();

                // 5. מנגנון גיבוי
                if (!matchedTherapists.Any())
                {
                    matchedTherapists = allTherapists.Take(3).ToList();
                }

                // 6. שמירה להיסטוריית צ'אטים (SQL)
                ChatSession historyRecord = new ChatSession
                {
                    PatientId = request.PatientId,
                    // המרה לסטרינג כי שינינו את סוג המשתנה במחלקה ChatSession
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    Summary = summary,
                    MessageCount = 1,

                    // שליפת השם המלא של המטפל הראשון שנמצא (במקום ה-Id שלו)
                    TherapistName = matchedTherapists.FirstOrDefault()?.FullName ?? "General Support"
                };

                historyRecord.Save();

                // 7. החזרת תשובה ל-React
                return Ok(new
                {
                    message = "Triage processed and saved to history",
                    patientSummary = summary,
                    matches = matchedTherapists,
                    riskLevel = request.AnswersText.ToLower().Contains("crisis") ? "High" : "Standard"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // פונקציית עזר לחישוב דמיון בין וקטורים
        private double CalculateCosineSimilarity(float[] vecA, float[] vecB)
        {
            if (vecA == null || vecB == null || vecA.Length != vecB.Length) return 0;
            double dotProduct = 0, magA = 0, magB = 0;
            for (int i = 0; i < vecA.Length; i++)
            {
                dotProduct += vecA[i] * vecB[i];
                magA += Math.Pow(vecA[i], 2);
                magB += Math.Pow(vecB[i], 2);
            }
            return dotProduct / (Math.Sqrt(magA) * Math.Sqrt(magB));
        }
    }

    public class TriageRequest
    {
        public int PatientId { get; set; }
        public string AnswersText { get; set; }
    }

    public class ChatRequest
    {
        public int PatientId { get; set; }
        public List<ChatMessageDto> Messages { get; set; }
    }

    public class ChatMessageDto
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}