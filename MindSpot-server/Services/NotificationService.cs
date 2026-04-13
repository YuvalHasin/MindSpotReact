using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Builder.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class NotificationService
{
    private static bool _isInitialized = false;

    public NotificationService(string pathToKey)
    {
        if (!_isInitialized)
        {
            // טעינת הקובץ שהורדת מ-Firebase
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile(pathToKey)
            });
            _isInitialized = true;
        }
    }

    public async Task SendPushNotification(string token, string title, string body)
    {
        var message = new Message()
        {
            Token = token, // ה"כתובת" של המכשיר של האדמין
            Notification = new Notification()
            {
                Title = title,
                Body = body
            },
            Data = new Dictionary<string, string>()
            {
                { "click_action", "FLUTTER_NOTIFICATION_CLICK" }, // חשוב לריאקט
                { "id", "1" }
            }
        };

        // שליחה בפועל דרך השרתים של גוגל
        string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
        Console.WriteLine("Successfully sent message: " + response);
    }
}