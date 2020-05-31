using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using Airline.BaggageInfoBot.Web.Helper;
using Airline.BaggageInfoBot.Web.Model;
using Airline.BaggageInfoBot.Web.Repository;

namespace Airline.BaggageInfoBot.Web
{
    [Serializable]
    public class EchoBot: IDialog<object>
    {
        public static Dictionary<string, string> privateStorage = new Dictionary<string, string>();
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {

            var message = await argument;
            switch (message.Type)
            {
                case ActivityTypes.Message:
                    await HandleMessage(context, message);
                    break;

                case ActivityTypes.Invoke:
                    await HandleInvoke(context, message);
                    break;
                default:
                    break;
            }

            
            
        }

        private async Task HandleInvoke(IDialogContext context, IMessageActivity message)
        {
            var activity = (Activity)message;
            var connectorClient = new ConnectorClient(new Uri(activity.ServiceUrl));

            // Get O365 connector card query data.
            O365ConnectorCardActionQuery o365CardQuery = activity.GetO365ConnectorCardActionQueryData();
            Activity replyActivity = activity.CreateReply();
            switch (o365CardQuery.ActionId)
            {
                case Constants.PNR:
                    var PNRno = Newtonsoft.Json.JsonConvert.DeserializeObject<O365BodyValue>(o365CardQuery.Body);
                    await AttachBaggagebyPNR(PNRno.Value, replyActivity);
                    context.ConversationData.RemoveValue(LastMessageIdKey);
                    break;
                case Constants.TicketNumber:
                    var Ticketno = Newtonsoft.Json.JsonConvert.DeserializeObject<O365BodyValue>(o365CardQuery.Body);
                    await AttachBaggageInformationTicket(Ticketno.Value, replyActivity);
                    context.ConversationData.RemoveValue(LastMessageIdKey);
                    break;
                case Constants.Name:
                    var Name = Newtonsoft.Json.JsonConvert.DeserializeObject<O365BodyValue>(o365CardQuery.Body);
                    await AttachBaggageInformationName(Name.Value, replyActivity);
                    context.ConversationData.RemoveValue(LastMessageIdKey);
                    break;
                case Constants.DetailsofCheckedBaggage:
                    var PNRno1 = Newtonsoft.Json.JsonConvert.DeserializeObject<O365BodyValue>(o365CardQuery.Body).Value;
                    await AttachBaggageInformation(PNRno1, replyActivity);
                    break;
                case Constants.CurrentStatus:
                    var PNRno2 = Newtonsoft.Json.JsonConvert.DeserializeObject<O365BodyValue>(o365CardQuery.Body);
                    await AttachBaggageInformation(PNRno2.Value, replyActivity);
                    break;
                case Constants.RebookBaggage:
                    RebookClass NewFlightTicketNumber = Newtonsoft.Json.JsonConvert.DeserializeObject<RebookClass>(o365CardQuery.Body);
                    await AttachRebookInformation(NewFlightTicketNumber.flightNumberInput, replyActivity);
                    context.ConversationData.RemoveValue(LastMessageIdKey);
                    break;
                case Constants.ReportMissing:
                    await AttachReportMissing(replyActivity);
                    context.ConversationData.RemoveValue(LastMessageIdKey);
                    break;
                default:
                    break;
            }

            string savedMessageId;
            if (context.ConversationData.TryGetValue(LastMessageIdKey, out savedMessageId))
            {
                try
                {
                    var resource = await connectorClient.Conversations.UpdateActivityAsync(replyActivity.Conversation.Id, savedMessageId, replyActivity);
                    savedMessageId = resource.Id;
                }
                catch (Exception e)
                {
                    var resource = e.Message.ToString();
                }
            }
            else
            {
                var resource = await connectorClient.Conversations.ReplyToActivityWithRetriesAsync(replyActivity);
                savedMessageId = resource.Id;
            }
            context.ConversationData.SetValue(LastMessageIdKey, savedMessageId);
        }

        private static readonly string LastMessageIdKey = "LastMessageId";

        private static async Task AddListCardAttachment(Activity replyActivity, System.Collections.Generic.IEnumerable<Baggage> baggages)
        {
            var card = O365CardHelper.GetListCardAttachment(baggages);
            try
            {
                replyActivity.Attachments.Add(card);
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);
            }
        }
        private static async Task AttachBaggageInformationName(string Name, Activity replyActivity)
        {
            var actionId = Guid.NewGuid().ToString();
            var list = await DocumentDBRepository<Baggage>.GetItemsAsync(d => d.Name.ToLower().Contains(Name.ToLower()));
            int count = list.Count();
            if (list.Count() > 1)
            {
                var ListCard = AddListCardAttachment(replyActivity, list);
            }
            else if (list.Count() == 1)
            {
                var replyCard = O365CardHelper.GetO365ConnectorCard(list.FirstOrDefault());
                replyActivity.Attachments.Add(replyCard.ToAttachment());
            }
            else
            {
                replyActivity.Text = $"Passenger with Name {Name} does not exists.";
            }


        }

        private async Task HandleMessage(IDialogContext context, IMessageActivity message)
        {
            if (message.Text != null && message.Text.Contains("Show Baggage by Name"))
            {

                var reply = context.MakeMessage();
                var PNRToSearch = System.Text.RegularExpressions.Regex.Match(message.Text, @"\(([^)]*)\)").Groups[1].Value;
                var list = await DocumentDBRepository<Baggage>.GetItemsAsync(d => d.PNR.ToLower() == PNRToSearch.ToLower());
                var BaggagebyPNR = O365CardHelper.GetO365ConnectorCard(list.FirstOrDefault());

                reply.Attachments.Add(BaggagebyPNR.ToAttachment());
                await context.PostAsync((reply));

            }
            else
            {
                var messageText = message.Text.ToLower();
                var reply = context.MakeMessage();
                reply.Attachments.Add(GetCardsInformation());

                await context.PostAsync((reply));
                context.Wait(MessageReceivedAsync);
            }
            context.ConversationData.RemoveValue(LastMessageIdKey);
        }
        private static async Task AttachBaggagebyPNR(string PNR, Activity replyActivity)
        {
            var actionId = Guid.NewGuid().ToString();
            var list = await DocumentDBRepository<Baggage>.GetItemsAsync(d => d.PNR.ToLower() == PNR.ToLower());
            if (list.Count() == 0)
            {
                replyActivity.Text = "The passenger with PNR " + PNR + " does not have any checked in baggage.";

            }
            else
            {
                var BaggagebyPNR = O365CardHelper.GetO365ConnectorCard(list.FirstOrDefault());
                
                replyActivity.Attachments.Add(BaggagebyPNR.ToAttachment());
            }
        }

        private static async Task AttachBaggageInformationTicket(string Ticket, Activity replyActivity)
        {
            var actionId = Guid.NewGuid().ToString();
            var list = await DocumentDBRepository<Baggage>.GetItemsAsync(d => d.TicketNo.ToLower() == Ticket.ToLower());
            if (list.Count() == 0)
            {
                replyActivity.Text = "The passenger with ticket " + Ticket + " does not have any checked in baggage.";
            }
            else
            {
                var replyCard = O365CardHelper.GetO365ConnectorCard(list.FirstOrDefault());
                
                replyActivity.Attachments.Add(replyCard.ToAttachment());
            }
        }
        private static async Task AttachRebookInformation(string FlightNumber, Activity replyActivity)
        {

            replyActivity.Text = "Your Baggage is rebooked with Flight Number: " + FlightNumber;
        }
       

        private static async Task AttachBaggageInformation(string PNR, Activity replyActivity)
        {

            var list = await DocumentDBRepository<Baggage>.GetItemsAsync(d => d.PNR.ToLower()==PNR.ToLower());
            if (list.Count() == 0)
            {
                replyActivity.Text = "Baggage with PNR " + PNR + " not found in the system.";
            }
            else
            {
                var replyCard = O365CardHelper.GetO365ConnectorCardResult(list.FirstOrDefault());
                replyActivity.Attachments.Add(replyCard.ToAttachment());
            }
        }

        private static async Task AttachReportMissing(Activity replyActivity)
        {
            Random random = new Random();
            int TicketNumberref = random.Next();
            replyActivity.Text = "Report registered. Reference ticket number is: " + TicketNumberref + " The passenger has been notified on contact number provided with the booking.";
        }
        public static Attachment GetCardsInformation()
        {
            var section = new O365ConnectorCardSection {
                ActivityTitle= "Track customer belongings",
                Text = "Using this bot you can<ol><li>Check baggage status</li><li>Track current location</li><li>Re-assign baggage</li><li>Report missing</li></ol> Choose one of the options below to retrieve details"
            };
                     
            var PNRNumberCard = new O365ConnectorCardActionCard(
                O365ConnectorCardActionCard.Type,
                "Baggage by PNR",
                "PNR",
                new List<O365ConnectorCardInputBase>
                {
                    new O365ConnectorCardTextInput(
                        O365ConnectorCardTextInput.Type,
                        "pnrNumberInput",
                        true,
                        "Enter PNR number (Ex:DBW6WK, DBW6WF, DBW6RK)",
                        null,
                        false,
                        null)
                },
                new List<O365ConnectorCardActionBase>
                {
                    new O365ConnectorCardHttpPOST(
                        O365ConnectorCardHttpPOST.Type,
                        "Show",
                        Constants.PNR,
                        @"{""Value"":""{{pnrNumberInput.value}}""}")
                });
            var TicketNumnerCard = new O365ConnectorCardActionCard(
                O365ConnectorCardActionCard.Type,
                "Baggage by Ticket#",
                "Ticket Number",
                new List<O365ConnectorCardInputBase>
                {
                    new O365ConnectorCardTextInput(
                        O365ConnectorCardTextInput.Type,
                        "ticketNumberInput",
                        true,
                        "Enter Ticket number (Ex: DBW612, DBW634, DBW678)",
                        null,
                        false,
                        null)
                },
                new List<O365ConnectorCardActionBase>
                {
                    new O365ConnectorCardHttpPOST(
                        O365ConnectorCardHttpPOST.Type,
                        "Show",
                        Constants.TicketNumber,
                        @"{""Value"":""{{ticketNumberInput.value}}""}")
                });
            var NameCard = new O365ConnectorCardActionCard(
                O365ConnectorCardActionCard.Type,
                "Baggage by Passenger Name",
                "Name",
                new List<O365ConnectorCardInputBase>
                {
                    new O365ConnectorCardTextInput(
                        O365ConnectorCardTextInput.Type,
                        "NameInput",
                        true,
                        "Enter Passenger Name( Ex: Adele, Alex, Allan)",
                        null,
                        false,
                        null)
                },
                new List<O365ConnectorCardActionBase>
                {
                    new O365ConnectorCardHttpPOST(
                        O365ConnectorCardHttpPOST.Type,
                        "Show",
                        Constants.Name,
                        @"{""Value"":""{{NameInput.value}}""}")
                });

            


            O365ConnectorCard card = new O365ConnectorCard()
            {
                ThemeColor = "#E67A9E",
                Title = "Passenger Baggage Information",
                Sections = new List<O365ConnectorCardSection> { section },
                PotentialAction = new List<O365ConnectorCardActionBase>
                {
                    PNRNumberCard,
                    TicketNumnerCard,
                    NameCard
                }
            };
            return card.ToAttachment();

        }

        
    }
}
