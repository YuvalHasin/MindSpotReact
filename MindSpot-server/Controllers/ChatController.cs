using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindSpot_server.Services;
using MindSpot_server.Models; // הוספת ה-using הזה פותרת את שגיאת ה-ChatRequest
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Patient")]
    public class ChatController : ControllerBase
    {
        private readonly OpenAiService _openAiService;

        public ChatController(OpenAiService openAiService)
        {
            _openAiService = openAiService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            // בדיקת תקינות בסיסית - לוודא שיש הודעות בבקשה
            if (request == null || request.Messages == null || request.Messages.Count == 0)
                return BadRequest("Invalid chat request.");

            try
            {
                // 1. הכנת רשימת ההודעות עבור OpenAI
                var chatMessages = new List<ChatMessage>();

                // הוספת הוראת מערכת (System Prompt) כדי לקבוע את האישיות של הבוט
                chatMessages.Add(new SystemChatMessage(
                    "You are Serenity, a supportive and empathetic AI wellness companion for MindSpot. " +
                    "Your goal is to listen, provide emotional support, and help users feel heard. " +
                    "Keep your responses warm, concise, and professional. " +
                    "If a user is in crisis, gently remind them to contact professional help."
                ));

                // 2. המרת היסטוריית השיחה מה-DTO לפורמט של OpenAI
                foreach (var msg in request.Messages)
                {
                    if (msg.Role.ToLower() == "user")
                        chatMessages.Add(new UserChatMessage(msg.Content));
                    else if (msg.Role.ToLower() == "assistant")
                        chatMessages.Add(new AssistantChatMessage(msg.Content));
                }

                // 3. שליחה ל-OpenAI דרך השירות
                var response = await _openAiService.GetChatResponseAsync(chatMessages);

                // 4. החזרת התשובה ל-React
                return Ok(new
                {
                    role = "assistant",
                    content = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }
    }
}