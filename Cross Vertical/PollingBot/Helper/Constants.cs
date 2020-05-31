using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CrossVertical.PollingBot.Helper
{
    public static class Constants
    {
         public const string PublishSurvey = "Publish Survey";
        public const string SubmitSurvey = "Submit Survey";
        public const string Submit = "Submit";
        public const string NewUser = "New User";
        public const string FeedBack = "FeedBack";
        public const string QuestionBank = "QuestionBank";
        public const string SetAdmin = "Admin";

    }

    public class InputDetails
    {
        public string Type { get; set; }
        public string QuestionBankId { get; set; }

        public string Options { get; set; }
        public string QuestionId { get; set; }

        public string txtAdmin { get; set; }

    }

    
}