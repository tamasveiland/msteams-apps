using Microsoft.Bot.Connector.Teams.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Airline.BaggageInfoBot.Web.Model;
using Microsoft.Bot.Connector;
namespace Airline.BaggageInfoBot.Web.Helper
{
    public class O365CardHelper
    {

        public static O365ConnectorCard GetO365ConnectorCard(Baggage baggage)
        {
            var section = new O365ConnectorCardSection
            {
                ActivityTitle = $"Name: **{baggage.Name}**",
                ActivitySubtitle = $"Ticket: **{baggage.TicketNo}**    PNR: **{baggage.PNR}**",
                ActivityImage = "https://airlinebaggage.azurewebsites.net/resources/" + SplitName(baggage.Name) + ".jpg"
            };

            var RebookingCard = new O365ConnectorCardActionCard(
                O365ConnectorCardActionCard.Type,
                "Rebook Baggage",
                "PNR",
                new List<O365ConnectorCardInputBase>
                {
                    new O365ConnectorCardTextInput(
                        O365ConnectorCardTextInput.Type,
                        "flightNumberInput",
                        true,
                        "Enter new flight number(Ex: 220, 350, 787)",
                        null,
                        false,
                        null)
                },
                new List<O365ConnectorCardActionBase>
                {
                    new O365ConnectorCardHttpPOST(
                        O365ConnectorCardHttpPOST.Type,
                        "Rebook",
                        Constants.RebookBaggage,
                        @"{""flightNumberInput"":""{{flightNumberInput.value}}""/*, ""pnrNumberInput"": ""{{pnrNumberInput.value}}""*/}")
                });
            O365ConnectorCard card = new O365ConnectorCard()
            {
                Title = "What do you want to do?",
                ThemeColor = "#E67A9E",
                Sections = new List<O365ConnectorCardSection> { section },
                PotentialAction = new List<O365ConnectorCardActionBase>
                {
                    new O365ConnectorCardHttpPOST(O365ConnectorCardHttpPOST.Type,"Show Baggage Details",Constants.CurrentStatus,$"{{'Value':'{baggage.PNR}'}}"),
                    RebookingCard,
                    new O365ConnectorCardHttpPOST(O365ConnectorCardHttpPOST.Type,"Report Missing",Constants.ReportMissing,baggage.PNR)

                }
            };
            return card;
        }

        public static string SplitName(string Name)
        {
            if (Name != null)
            {
                var str = Name.Split();
                var FirstName = str[0];
                return FirstName;
            }
            else
            {
                return Name;
            }
        }
        public static O365ConnectorCard GetO365ConnectorCardResult(Baggage baggage)
        {
            var section = new O365ConnectorCardSection
            {
                ActivityTitle = $"Name: **{baggage.Name}**",
                ActivitySubtitle = $"Ticket: **{baggage.TicketNo}**    PNR: **{baggage.PNR}**",
                ActivityImage = "https://airlinebaggage.azurewebsites.net/resources/" + SplitName(baggage.Name) + ".jpg",
                Facts = new List<O365ConnectorCardFact>
                    {

                        new O365ConnectorCardFact("From", baggage.From),
                        new O365ConnectorCardFact("To", baggage.To),
                        new O365ConnectorCardFact("Number of Checked in Bags",baggage.BagCount.ToString()),
                        new O365ConnectorCardFact("Current Status",baggage.CurrentStatus),
                        new O365ConnectorCardFact("Baggage Identifer Number", baggage.BaggageIdentifer.ToString())
                    }
            };
            var RebookingCard1 = new O365ConnectorCardActionCard(
                O365ConnectorCardActionCard.Type,
                "Rebook Baggage",
                "PNR",
                new List<O365ConnectorCardInputBase>
                {
                    new O365ConnectorCardTextInput(
                        O365ConnectorCardTextInput.Type,
                        "flightNumberInput",
                        true,
                        "Enter new flight number (Ex: 220, 350, 787)",
                        null,
                        false,
                        null)
                },
                new List<O365ConnectorCardActionBase>
                {
                    new O365ConnectorCardHttpPOST(
                        O365ConnectorCardHttpPOST.Type,
                        "Rebook",
                        Constants.RebookBaggage,
                        @"{""flightNumberInput"":""{{flightNumberInput.value}}""/*, ""pnrNumberInput"": ""{{pnrNumberInput.value}}""*/}")

                });
            O365ConnectorCard card = new O365ConnectorCard()
            {
                Title = "Baggage Details",
                ThemeColor = "#E67A9E",
                Sections = new List<O365ConnectorCardSection> { section },
                PotentialAction = new List<O365ConnectorCardActionBase>
                {
                    RebookingCard1,
                    new O365ConnectorCardHttpPOST(O365ConnectorCardHttpPOST.Type,"Report Missing",Constants.ReportMissing,baggage.PNR)

                }

            };
            return card;
        }
        public static Attachment GetListCardAttachment(IEnumerable<Baggage> baggages)
        {
            var listCard = new ListCard();
            listCard.content = new Content();
            
            var list = new List<Item>();
            foreach (var baggage in baggages)
            {
                

                var item = new Item();
                item.icon = baggage.Gender == "Male" ? "https://airlinebaggage.azurewebsites.net/resources/" + SplitName(baggage.Name) + ".jpg" : "https://airlinebaggage.azurewebsites.net/resources/" + SplitName(baggage.Name) + ".jpg";
                item.type = "resultItem";
                item.id = baggage.PNR;
                item.title = baggage.Name;
                item.subtitle = "PNR: " + baggage.PNR + "   |" + " Ticket: " + baggage.TicketNo + "  |" + " Seat:  " + baggage.SeatNo;

                item.tap = new Tap()
                {
                    type = "imBack",
                    title = "Aircraft",
                    value = "Show Baggage by Name " + baggage.Name + " (" + baggage.PNR + ")"
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

    public class RebookClass
    {
        public string flightNumberInput { get; set; }
        public string pnrNumberInput { get; set; }

        public string ActionId { get; set; }
    }
}