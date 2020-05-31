using System;
using System.Collections.Generic;

using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using Airline.FleetInfoBot.Web.Helper;
using Airline.FleetInfoBot.Web.Repository;
using Airline.FleetInfoBot.Web.Model;
using System.Linq;
using System.Configuration;

namespace Airline.FleetInfoBot.Web
{
    [Serializable]
    public class EchoBot: IDialog<object>

    {
        private static Dictionary<string, string> privateStorage = new Dictionary<string, string>();
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


            //var message = await argument;
            

        }

        private async Task HandleMessage(IDialogContext context, IMessageActivity message)
        {
            if (message.Text != null && message.Text.Contains("Show aircraft by Id"))
            {

                //var reply = context.MakeMessage();
                var reply = (Activity)message;
                Activity replyActivity = reply.CreateReply();
                var actionId = Guid.NewGuid().ToString();
                var flightnumber = System.Text.RegularExpressions.Regex.Match(message.Text, @"\(([^)]*)\)").Groups[1].Value;
                var list = await DocumentDBRepository<AirCraftInfo>.GetItemsAsync(d => d.AircraftId == flightnumber);
                var aircraftInfoCard = O365CardHelper.GetO365ConnectorCardResult(list.FirstOrDefault(), actionId);

                replyActivity.Attachments.Add(aircraftInfoCard.ToAttachment());
                ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
                var msgToUpdate = await connector.Conversations.ReplyToActivityAsync(replyActivity);
                context.ConversationData.SetValue(actionId, msgToUpdate.Id);
                privateStorage.Add(actionId, msgToUpdate.Id);
                //await context.PostAsync((replyActivity));

            }
            else
            {
                var messageText = message.Text.ToLower();
                var reply = context.MakeMessage();

                reply.Attachments.Add(GetCardsInformation());

                await context.PostAsync((reply));
                context.Wait(MessageReceivedAsync);
            }
        }

        private async Task HandleInvoke(IDialogContext context, IMessageActivity message)
        {
            var activity = (Activity)message;
            var connectorClient = new ConnectorClient(new Uri(activity.ServiceUrl));
            //string savedMessageId;
            // Get O365 connector card query data.
            O365ConnectorCardActionQuery o365CardQuery = activity.GetO365ConnectorCardActionQueryData();
            AirCraftDetails actionInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<AirCraftDetails>(o365CardQuery.Body);
            Activity replyActivity = activity.CreateReply();
           
            
                switch (o365CardQuery.ActionId)
                {
                    case Constants.Assignaircraft:
                        AirCraftDetails assignairCraftInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<AirCraftDetails>(o365CardQuery.Body);
                        

                        await AttachAssignairCraft(assignairCraftInfo, replyActivity);
                        break;
                    case Constants.MarkGrounded:
                        AirCraftDetails groundedAirCraftInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<AirCraftDetails>(o365CardQuery.Body);
                       
                        await MarkGroundedAirCraft(groundedAirCraftInfo, replyActivity);
                        break;
                    case Constants.Available:
                        AirCraftDetails freeAirCraftInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<AirCraftDetails>(o365CardQuery.Body);
                        
                        await MarkFreeAirCraft(freeAirCraftInfo, replyActivity);
                        break;
                    default:
                        break;

                }
            
            
            
                var lastMessageId = context.ConversationData.GetValueOrDefault<string>(actionInfo.ActionId);
                if (lastMessageId == null && privateStorage.ContainsKey(actionInfo.ActionId))
                    lastMessageId = privateStorage[actionInfo.ActionId];
                if (!string.IsNullOrEmpty(lastMessageId))
                {
                    // Update existing item.
                    await connectorClient.Conversations.UpdateActivityAsync(replyActivity.Conversation.Id, lastMessageId, replyActivity);
                    context.ConversationData.RemoveValue(actionInfo.ActionId);
                    //if (privateStorage.ContainsKey(Actionid))
                    //    privateStorage.Remove(Actionid);
                }
                else
                {
                    await connectorClient.Conversations.SendToConversationAsync(replyActivity);
                }
            
            
        }

        private static async Task AttachAssignairCraft(AirCraftDetails aircardInfo, Activity replyActivity)
        {


            var aircraftInfo = await DocumentDBRepository<AirCraftInfo>.GetItemsAsync(d => d.FlightNumber == aircardInfo.FlightNumber && d.AircraftId == aircardInfo.AircraftId);

            if (aircraftInfo.Count() > 0)
            {
                try
                {
                    var list = aircraftInfo.FirstOrDefault();

                    list.Status = Status.Assigned;
                    var aircraftDetails = await DocumentDBRepository<AirCraftInfo>.UpdateItemAsync(list.Id, list);
                    var replyCard = O365CardHelper.GetO365ConnectorCardResult(aircraftInfo.FirstOrDefault(), aircardInfo.ActionId);
                    replyActivity.Attachments.Add(replyCard.ToAttachment());
                    //replyActivity.Text = $"Aircraft {aircardInfo.AircraftId} has been assigned to Flight: {aircardInfo.FlightNumber}";
                }
                catch (Exception e)
                {
                    replyActivity.Text = e.Message.ToString();
                }
            }
        }

        private static async Task MarkGroundedAirCraft(AirCraftDetails aircardInfo, Activity replyActivity)
        {
            var aircraftInfo = await DocumentDBRepository<AirCraftInfo>.GetItemsAsync(d => d.FlightNumber == aircardInfo.FlightNumber && d.AircraftId == aircardInfo.AircraftId);

            if (aircraftInfo.Count() > 0)
            {
                try
                {
                    var list = aircraftInfo.FirstOrDefault();

                    list.Status = Status.Grounded;
                    var aircraftDetails = await DocumentDBRepository<AirCraftInfo>.UpdateItemAsync(list.Id, list);
                    var replyCard = O365CardHelper.GetO365ConnectorCardResult(aircraftInfo.FirstOrDefault(), aircardInfo.ActionId);
                    replyActivity.Attachments.Add(replyCard.ToAttachment());
                    //replyActivity.Text = $"Aircraft {aircardInfo.AircraftId} has been grounded";
                }
                catch (Exception e)
                {
                    replyActivity.Text = e.Message.ToString();
                }
            }

        }

        private static async Task MarkFreeAirCraft(AirCraftDetails aircardInfo, Activity replyActivity)
        {
            var aircraftInfo = await DocumentDBRepository<AirCraftInfo>.GetItemsAsync(d => d.FlightNumber == aircardInfo.FlightNumber && d.AircraftId == aircardInfo.AircraftId);

            if (aircraftInfo.Count() > 0)
            {
                try
                {
                    var list = aircraftInfo.FirstOrDefault();

                    list.Status = Status.Available;
                    var aircraftDetails = await DocumentDBRepository<AirCraftInfo>.UpdateItemAsync(list.Id, list);
                    var replyCard = O365CardHelper.GetO365ConnectorCardResult(aircraftInfo.FirstOrDefault(), aircardInfo.ActionId);
                    replyActivity.Attachments.Add(replyCard.ToAttachment());
                    //replyActivity.Text = $"Aircraft {aircardInfo.AircraftId} is available";
                }
                catch (Exception e)
                {
                    replyActivity.Text = e.Message.ToString();
                }
            }

        }

        public static Attachment GetCardsInformation()
        {

            var section = new O365ConnectorCardSection
            {
                ActivityTitle = "Your one stop destination to managing your fleet",
                
            };
            
            
            var AirCraftInfo = new O365ConnectorCardActionCard(
                O365ConnectorCardActionCard.Type,
                "Show me aircraft details",
                "Multiple Choice Card",
                new List<O365ConnectorCardInputBase>
                {
                    new O365ConnectorCardTextInput(
                        O365ConnectorCardTextInput.Type,
                        "flightNumberInput",
                        true,
                        "Enter flight number Ex: 320,777,220",
                        null,
                        false,
                        null),
                     new O365ConnectorCardTextInput(
                        O365ConnectorCardTextInput.Type,
                        "baselocationInput",
                        true,
                        "Enter Base Location Ex: Seattle",
                        null,
                        false,
                        null)

               },
               

            new List<O365ConnectorCardActionBase>
                  {
                   new O365ConnectorCardHttpPOST(
                        O365ConnectorCardHttpPOST.Type,
                        "Show me aircraft details",
                        Constants.ShowAirCraftDetails,
                        @"{""FlightNumber"":""{{flightNumberInput.value}}"", ""BaseLocation"":""{{baselocationInput.value}}""}")
                 });

            O365ConnectorCard card = new O365ConnectorCard()
            {
                ThemeColor = "#E67A9E",
                Title = "Fleet Management",
                Sections = new List<O365ConnectorCardSection> { section },
                PotentialAction = new List<O365ConnectorCardActionBase>
                {
                    AirCraftInfo
                    
                }
            };
            return card.ToAttachment();

        }
    }
}
