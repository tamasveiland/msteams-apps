using Airline.PassengerInfo.Web.Model;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams.Models;
using System.Collections.Generic;

namespace Airline.PassengerInfo.Web.Helper
{
    public class O365CardHelper
    {
        public const string BaseUrl = "https://contosoairline.azurewebsites.net"; //"https://50f24fb4.ngrok.io";
        public static O365ConnectorCard GetO365ConnectorCard(Passenger passenger)
        {
            var section = new O365ConnectorCardSection
            {
                Title = "Passenger Details",
                ActivityTitle = passenger.Name,
                ActivitySubtitle = $"Flight: **{passenger.FlightNumber}**    Seat: **{passenger.Seat}**    Gate= **{passenger.GateNumber}**",
                ActivityText = $"Special Instruction: { (string.IsNullOrEmpty(passenger.SpecialAssistance) ? "None" : passenger.SpecialAssistance)}",
                ActivityImage = BaseUrl + $"/public/resources/{GetPictureName(passenger.Name)}.jpg",
                Facts = new List<O365ConnectorCardFact>
                    {
                        new O365ConnectorCardFact("From", passenger.From ),
                        new O365ConnectorCardFact("To", passenger.To),
                        new O365ConnectorCardFact("Date", passenger.DepartureTime.ToString("d MMM yy")),
                        new O365ConnectorCardFact("Class", passenger.Class),
                        new O365ConnectorCardFact("PNR", passenger.PNR),
                        new O365ConnectorCardFact("Frequent Flyer", string.IsNullOrEmpty(passenger.FrequentFlyerNumber)? "No" : passenger.FrequentFlyerNumber ),
                        new O365ConnectorCardFact("Loyalty Status", passenger.LoyaltyStatus.ToString())
                    }
            };

            O365ConnectorCard card = new O365ConnectorCard()
            {
                ThemeColor = "#E67A9E",
                Sections = new List<O365ConnectorCardSection> { section },
            };
            return card;
        }

        public static string GetPictureName(string name)
        {
            var nameParts = name.Split(' ');
            var firstName = nameParts[0];
            var secondName = nameParts[1];
            return firstName + secondName[0];
        }

        public static ThumbnailCard GetPreviewCard(Passenger passenger)
        {
            var preview = new ThumbnailCard
            {
                Title = passenger.Name,
                Text = "Seat: " + passenger.Seat + (passenger.LoyaltyStatus == LoyaltyStatus.None ? "" : $" | {passenger.LoyaltyStatus.ToString()} member") + (string.IsNullOrEmpty(passenger.SpecialAssistance) ? "" : " | Special Assistance Required"),
            };
            preview.Images = new List<CardImage>();
            preview.Images.Add(new CardImage(BaseUrl + $"/public/resources/{GetPictureName(passenger.Name)}.jpg"));
            return preview;
        }

        public static Attachment GetListCardAttachment(IEnumerable<Passenger> passengers, string title)
        {
            var listCard = new ListCard();
            listCard.content = new Content();
            listCard.content.title = title;
            var list = new List<Item>();
            foreach (var passenger in passengers)
            {
                var item = new Item();
                item.icon = BaseUrl + $"/public/resources/{GetPictureName(passenger.Name)}.jpg";
                item.type = "resultItem";
                item.id = passenger.PNR;
                item.title = passenger.Name;
                item.subtitle = "Seat: " + passenger.Seat + (passenger.LoyaltyStatus == LoyaltyStatus.None ? "" : $" | {passenger.LoyaltyStatus.ToString()} member") + (string.IsNullOrEmpty(passenger.SpecialAssistance) ? "" : " | Special Assistance Required");
                item.tap = new Tap()
                {
                    type = "imBack",
                    title = "PNR",
                    value = "Show Passenger " + passenger.Name + " (" + passenger.PNR + ")"
                };
                list.Add(item);
            }

            listCard.content.items = list.ToArray();

            Attachment attachment = new Attachment();
            attachment.ContentType = listCard.contentType;
            attachment.Content = listCard.content;
            return attachment;
        }



    }
    public class SearchByPNRData
    {
        public string SearchByPNR { get; set; }
    }

    public class O365BodyValue
    {
        public string Value { get; set; }
    }
}