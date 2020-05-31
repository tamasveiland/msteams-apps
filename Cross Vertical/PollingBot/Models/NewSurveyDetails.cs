using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CrossVertical.PollingBot.Models
{
    public class NewSurveyDetails
    {
        public string Questions { get; set; }
        public List<string> QuestionOptions { get; set;}
        public List<string> MemberEmails { get; set; } 

        public string Type { get; set; }
    }

    public class UserDetails
    {
        [JsonProperty(PropertyName = "userId")]
        public string UserId { get; set; }
        [JsonProperty(PropertyName = "emailId")]
        public string EmaildId { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "userName")]
        public string UserName { get; set; }
    }
    public class Admin
    {
        [JsonProperty(PropertyName = "emailId")]
        public string EmailId { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }

    public class QuestionBank
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "questions")]
        public List<Question> Questions { get; set; } = new List<Question>();
        [JsonProperty(PropertyName = "emailIds")]
        public List<string> EmailIds { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "active")]
        public bool Active { get; set; }
        
    }

    public class Question
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public List<string> Options { get; set; } = new List<string>();

        
    }
    public class FeedbackData
    {
        [JsonProperty(PropertyName = "userEmailId")]
        public string UserEmailId { get; set; }

        [JsonProperty(PropertyName = "questionBankId")]
        public string QuestionBankId { get; set; }
        [JsonProperty(PropertyName = "feedback")]
        public Dictionary<string, string> Feedback { get; set; } = new Dictionary<string, string>();
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "active")]
        public bool Active { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }

    public class ReadSurveyDetails
    {
        public string Question { get; set; }
        public string QuestionOptions { get; set; }
    }
}