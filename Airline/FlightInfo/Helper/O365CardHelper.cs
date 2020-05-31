using Microsoft.Bot.Connector.Teams.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Airline.FlightInfoBot.Web.Model;


using Microsoft.Bot.Connector;

namespace Airline.FlightInfoBot.Web.Helper
{
    public static class O365CardHelper
    {
        public static Attachment GetListofFlights(IEnumerable<FlightInfo> flights, DateTime JourneyDate)
        {
            var listCard = new ListCard();
            listCard.content = new Content();
            //listCard.content.title = "The following flights are avilibile on " + JourneyDate.ToShortDateString();
            var list = new List<Item>();
            int count = 0;
            foreach (var flight in flights)
            {
                DateTime journeydate = JourneyDate.Date;//.AddDays(1);
                DateTime dabasedate =(flight.JourneyDate.Date);
                if (journeydate.Date == dabasedate.Date)
                {
                    listCard.content.title = "The following flights are available on " + flight.JourneyDate.ToShortDateString();
                    count = 1;
                    var item = new Item();
                    item.icon = "https://flightinfobot.azurewebsites.net/resources/Airline-Flight-Bot-03.png";
                    item.type = "resultItem";
                    item.id = flight.FlightNumber;
                    item.title = "Flight: " + flight.FlightNumber + "   |" + " Departure: " + flight.Departure.ToShortTimeString() + "   |" + " Arrival: " + flight.Arrival.ToShortTimeString();
                    item.subtitle = "From: " + flight.FromCity + "   |" + " To: " + flight.ToCity + "  |" + " Seats:  " + flight.SeatCount;

                    item.tap = new Tap()
                    {
                        type = "imBack",
                        title = "FlightNumber",
                        value = "Show details of flight" + " (" + flight.FlightNumber + ")"
                    };
                    list.Add(item);
                }
               
            }
            if (count == 0)
            {
                listCard.content.title = "Flights not available for selected date. Please check some other date";
            }
            listCard.content.items = list.ToArray();

            Attachment attachment = new Attachment();
            attachment.ContentType = listCard.contentType;
            attachment.Content = listCard.content;
            return attachment;
        }
        public static O365ConnectorCard GetO365ConnectorCardResult(FlightInfo flight)
        {
            var section = new O365ConnectorCardSection
            {
                ActivityTitle = $"Flight number: **{flight.FlightNumber}**",
                ActivitySubtitle = $"PNR: **{flight.PNR}**",
                ActivityImage = "https://flightinfobot.azurewebsites.net/resources/Airline-Flight-Bot-03.png",
                Facts = new List<O365ConnectorCardFact>
                    {

                        new O365ConnectorCardFact("From", flight.FromCity),
                        new O365ConnectorCardFact("To", flight.ToCity),
                        new O365ConnectorCardFact("Date of Journey",flight.JourneyDate.ToShortDateString())

                    }
            };
            var PNRNumberCard = new O365ConnectorCardActionCard(
                O365ConnectorCardActionCard.Type,
                "Rebook the Passenger",
                "PNR",
                new List<O365ConnectorCardInputBase>
                {
                    new O365ConnectorCardTextInput(
                        O365ConnectorCardTextInput.Type,
                        "pnrNumberInput",
                        true,
                        "Enter PNR number",
                        null,
                        false,
                        null)
                },
                new List<O365ConnectorCardActionBase>
                {
                    new O365ConnectorCardHttpPOST(
                        O365ConnectorCardHttpPOST.Type,
                        "Rebook the Passenger",
                        Constants.Rebook,
                        @"{""pnrNumberInput"":""{{pnrNumberInput.value}}"", ""flightNumberInput"":"+ flight.FlightNumber+ " }")
                });




            O365ConnectorCard card = new O365ConnectorCard()
            {
                Title = "Rebook Passenger",
                ThemeColor = "#E67A9E",
                Sections = new List<O365ConnectorCardSection> { section },
                PotentialAction = new List<O365ConnectorCardActionBase>
                {
                    PNRNumberCard

                }


            };
            return card;
        }
    }

    

    public class FlightInputDetails
    {
        public string From { get; set; }
        public string To { get; set; }

        public DateTime JourneyDate { get; set; }
    }

    public class O365BodyValue
    {
        public string Value { get; set; }
    }

    public class RebookClass
    {
        public string flightNumberInput { get; set; }
        public string pnrNumberInput { get; set; }
    }

}