using AdaptiveCards;
using Microsoft.Bot.Connector;
using System;
using System.IO;

namespace ProfessionalServices.LeaveBot.Helpers
{
    /// <summary>
    ///  Helper class which posts to the saved channel every 20 seconds.
    /// </summary>
    public static class AdaptiveCardHelper
    {
        public static Attachment GetAdaptiveCard()
        {
            // Parse the JSON
            AdaptiveCardParseResult result = AdaptiveCard.FromJson(GetAdaptiveCardJson());

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = result.Card
            };
        }

        public static Attachment GetAdaptiveCardFromJosn(string json)
        {
            // Parse the JSON
            AdaptiveCardParseResult result = AdaptiveCard.FromJson(json);

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = result.Card
            };
        }

        public static String GetAdaptiveCardJson()
        {
            var path = System.Web.Hosting.HostingEnvironment.MapPath(@"~\Cards\AdaptiveCard.json");
            return File.ReadAllText(path);
        }
    }

    public static class TaskModuleUIConstants
    {
        public static UIConstants AdaptiveCard { get; set; } =
            new UIConstants(700, 500, "Adaptive Card: Inputs", "adaptivecard", "Adaptive Card");
    }

    public class UIConstants
    {
        public UIConstants(int width, int height, string title, string id, string buttonTitle)
        {
            Width = width;
            Height = height;
            Title = title;
            Id = id;
            ButtonTitle = buttonTitle;
        }

        public int Height { get; set; }
        public int Width { get; set; }
        public string Title { get; set; }
        public string ButtonTitle { get; set; }
        public string Id { get; set; }
    }
}