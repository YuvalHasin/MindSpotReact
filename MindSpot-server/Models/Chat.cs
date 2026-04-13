using MindSpot_server.DAL;
using System;
using System.Collections.Generic;

namespace MindSpot_server.Models
{
    public class ChatSession
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string CreatedAt { get; set; }
        public int MessageCount { get; set; }
        public string Summary { get; set; }
        public string TherapistName { get; set; }
        public int RecommendedTherapistId { get; set; }


        DBservices dbs = new DBservices();

        public int Save()
        {
            return dbs.InsertChatSession(this.PatientId, this.Summary, this.TherapistName);
        }
        public List<ChatSession> GetPatientHistory(int patientId)
        {
            return dbs.SelectChatSessionsByPatient(patientId);
        }
    }
}