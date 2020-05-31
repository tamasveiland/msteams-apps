using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using Microsoft.Bot.Connector.Teams.Models;
using Microsoft.Bot.Connector.Teams;
using System.Collections.Generic;
using Airline.PassengerInfo.Web.Helper;
using Airline.PassengerInfo.Web.Repository;
using Airline.PassengerInfo.Web.Model;

namespace Airline.PassengerInfo.Web
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;


            if (message.Text != null && message.Text.Contains("Show Passenger"))
            {
                // Show Passenger
                var PNRToSearch = System.Text.RegularExpressions.Regex.Match(message.Text, @"\(([^)]*)\)").Groups[1].Value;

                var passengers = await DocumentDBRepository<Passenger>.GetItemsAsync(d => d.PNR == PNRToSearch);
                var reply = context.MakeMessage();

                if (passengers.Count() == 0)
                {
                    reply.Text = $"Passenger with PNR {PNRToSearch} not found in system.";
                }
                else
                {
                    var replyCard = O365CardHelper.GetO365ConnectorCard(passengers.FirstOrDefault());
                    reply.Attachments.Add(replyCard.ToAttachment());
                }
                await context.PostAsync((reply));

            }
            else
            {
                var reply = context.MakeMessage();
                reply.Attachments.Add(GetFilter());

                await context.PostAsync((reply));
                context.Wait(MessageReceivedAsync);
            }
        }

        public Attachment GetFilter()
        {

            var section = new O365ConnectorCardSection("Your Passport to Airline Services from within Microsoft Teams", "Use PassengerInfoBot to do the following:\n<ul>\n<li>List <strong>passengers & their profiles</strong> for this flight</li>\n<li>List <strong>frequest flyers</strong> to serve them best</li>\n<li>Locate passengers with <strong>special assistance</strong>  needs</li>\n<li>Get all <strong>passengers seated in a specific zone</strong></li>\n<li>Find out <strong>who is seated</strong> at a specific location</li>\n</ul>\n\n\n\n", null, null, null, null);

            var classWiseCard = new O365ConnectorCardActionCard(
                O365ConnectorCardActionCard.Type,
                "Class",
                "classWiseCard",
                 new List<O365ConnectorCardInputBase>
                {
                    new  O365ConnectorCardMultichoiceInput(
                        O365ConnectorCardMultichoiceInput.Type,
                        "class",
                        true,
                        "Select class",
                        null,
                        new List<O365ConnectorCardMultichoiceInputChoice>
                        {
                            new O365ConnectorCardMultichoiceInputChoice("Business", "Business"),
                            new O365ConnectorCardMultichoiceInputChoice("Economy", "Economy")
                        },
                        "compact"
                        ,false)
                 },
                new List<O365ConnectorCardActionBase>
                {
                   new O365ConnectorCardHttpPOST(
                        O365ConnectorCardHttpPOST.Type,
                        "Show",
                        Constants.ClassWise,
                        @"{""Value"":""{{class.value}}""}")
                });

            var zoneWiseCard = new O365ConnectorCardActionCard(
                O365ConnectorCardActionCard.Type,
                "Passengers by Zone",
                "zoneWiseCard",
                 new List<O365ConnectorCardInputBase>
                {
                    new  O365ConnectorCardMultichoiceInput(
                        O365ConnectorCardMultichoiceInput.Type,
                        "zone",
                        true,
                        "Select Zone",
                        null,
                        new List<O365ConnectorCardMultichoiceInputChoice>
                        {
                            new O365ConnectorCardMultichoiceInputChoice("A", "A"),
                            new O365ConnectorCardMultichoiceInputChoice("B", "B"),
                            new O365ConnectorCardMultichoiceInputChoice("C", "C")
                        },
                        "compact"
                        ,false)
                 },
                new List<O365ConnectorCardActionBase>
                {
                   new O365ConnectorCardHttpPOST(
                        O365ConnectorCardHttpPOST.Type,
                        "Show",
                        Constants.Zone,
                        @"{""Value"":""{{zone.value}}""}")
                });

            var seatNumberCard = new O365ConnectorCardActionCard(
                O365ConnectorCardActionCard.Type,
                "Passenger by Seat #",
                "seatNumber",
                new List<O365ConnectorCardInputBase>
                {
                    new O365ConnectorCardTextInput(
                        O365ConnectorCardTextInput.Type,
                        "seatNumberInput",
                        true,
                        "Enter Seat Number",
                        null,
                        false,
                        null)
                },
                new List<O365ConnectorCardActionBase>
                {
                    new O365ConnectorCardHttpPOST(
                        O365ConnectorCardHttpPOST.Type,
                        "Show",
                        Constants.SeatNumber,
                        @"{""Value"":""{{seatNumberInput.value}}""}")
                });

            O365ConnectorCard card = new O365ConnectorCard()
            {
                ThemeColor = "#E67A9E",
                Title = "Welcome to PassengerInfoBot",
                Summary = "",
                Sections = new List<O365ConnectorCardSection> { section },
                PotentialAction = new List<O365ConnectorCardActionBase>
                {
                    new O365ConnectorCardHttpPOST(O365ConnectorCardHttpPOST.Type,"See All Passengers",Constants.All),
                    new O365ConnectorCardHttpPOST(O365ConnectorCardHttpPOST.Type,"See Frequent Flyers",Constants.FrequentFlyer),
                    new O365ConnectorCardHttpPOST(O365ConnectorCardHttpPOST.Type,"Passengers with Special Assistance",Constants.SpecialAssistance),
                    zoneWiseCard,
                    seatNumberCard,
                    classWiseCard,
                }
            };
            return card.ToAttachment();
        }


    }
}