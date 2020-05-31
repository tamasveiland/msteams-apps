using Microsoft.Bot.Connector.Teams.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Airline.FleetInfoBot.Web.Model;


using Microsoft.Bot.Connector;

namespace Airline.FleetInfoBot.Web.Helper
{
    public static class O365CardHelper
    {
        public static Attachment GetListofFlights(IEnumerable<AirCraftInfo> aircraftDetails)
        {
            var listCard = new ListCard();
            listCard.content = new Content();
            var list = new List<Item>();
           foreach (var aircraft in aircraftDetails)
            {
                listCard.content.title = "The following aircrafts are available at " + aircraft.BaseLocation; 
                    
                    var item = new Item();
                    item.icon = "https://fleetinfobot.azurewebsites.net/resources/Airline-Fleet-Bot-02.png";
                    item.type = "resultItem";
                    item.id = aircraft.FlightNumber;
                    item.title = "Aircraft ID: " + aircraft.AircraftId + "   |" + " Type: " + aircraft.FlightType;
                    item.subtitle = "Model: " + aircraft.Model + "   |" + " Capacity: " + aircraft.Capacity;

                item.tap = new Tap()
                    {
                        type = "imBack",
                        title = "Aircraft",
                        value = "Show aircraft by Id " + " (" + aircraft.AircraftId + ")"
                    };
                    list.Add(item);
                
               
            }
           
            listCard.content.items = list.ToArray();

            Attachment attachment = new Attachment();
            attachment.ContentType = listCard.contentType;
            attachment.Content = listCard.content;
            return attachment;
        }
        public static O365ConnectorCard GetO365ConnectorCardResult(AirCraftInfo flight, string actionId)
        {
            var section = new O365ConnectorCardSection
            {
                ActivityTitle = $"Aircraft Id: **{flight.AircraftId}**",
                ActivitySubtitle = $"Model: **{flight.Model}**",
                ActivityImage = "https://fleetinfobot.azurewebsites.net/resources/Airline-Fleet-Bot-02.png",
                Facts = new List<O365ConnectorCardFact>
                    {

                        new O365ConnectorCardFact("Base location", flight.BaseLocation),
                        new O365ConnectorCardFact("Capacity", flight.Capacity),
                        new O365ConnectorCardFact("Flight type",flight.FlightType),
                        new O365ConnectorCardFact("Status",flight.Status.ToString())

                    }
            };

            var actions = new List<O365ConnectorCardActionBase>();

            switch (flight.Status)
            {
                case Status.Available:
                    actions.Add(new O365ConnectorCardHttpPOST(O365ConnectorCardHttpPOST.Type, "Assign the new aircraft", Constants.Assignaircraft, $"{{'FlightNumber':'{flight.FlightNumber}','AircraftId':'{flight.AircraftId}', 'ActionId':'{actionId}'}}"));
                    actions.Add(new O365ConnectorCardHttpPOST(O365ConnectorCardHttpPOST.Type, "Mark as grounded", Constants.MarkGrounded, $"{{'FlightNumber':'{flight.FlightNumber}','AircraftId':'{flight.AircraftId}','ActionId':'{actionId}'}}"));
                    break;
                case Status.Assigned:
                    actions.Add(new O365ConnectorCardHttpPOST(O365ConnectorCardHttpPOST.Type, "Mark as grounded", Constants.MarkGrounded, $"{{'FlightNumber':'{flight.FlightNumber}','AircraftId':'{flight.AircraftId}', 'ActionId':'{actionId}'}}"));
                    break;
                case Status.Grounded:
                    actions.Add(new O365ConnectorCardHttpPOST(O365ConnectorCardHttpPOST.Type, "Mark as available", Constants.Available, $"{{'FlightNumber':'{flight.FlightNumber}','AircraftId':'{flight.AircraftId}', 'ActionId':'{actionId}'}}"));
                    break;
                default:
                    break;
            }
            O365ConnectorCard card = new O365ConnectorCard()
            {
                Title = "Assign an Aircraft",
                ThemeColor = "#E67A9E",
                Sections = new List<O365ConnectorCardSection> { section },
                PotentialAction = actions
            };
            return card;
        }
    }

    public class O365BodyValue
    {
        public string Value { get; set; }
    }

    public class AirCraftDetails
    {
        public string FlightNumber { get; set; }
        public string BaseLocation { get; set; }

        public string AircraftId { get; set; }

        public string ActionId { get; set; }

        public string Value { get; set; }
    }
}