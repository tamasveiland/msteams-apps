using System;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Airline.PassengerInfo.Web.Helper;
using Airline.PassengerInfo.Web.Model;
using Airline.PassengerInfo.Web.Repository;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;

namespace Airline.PassengerInfo.Web.Controllers
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {

        /// <summary>
        /// POST: api/Messages
        /// receive a message from a user and send replies
        /// </summary>
        /// <param name="activity"></param>
        [ResponseType(typeof(void))]
        public virtual async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            // check if activity is of type message
            if (activity != null && activity.GetActivityType() == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new RootDialog());
            }
            else if (activity.Type == ActivityTypes.Invoke) // Received an invoke
            {
                // Handle ComposeExtension query
                if (activity.IsComposeExtensionQuery())
                {
                    var response = await MessageExtension.HandleMessageExtensionQuery(activity);
                    return response != null
                        ? Request.CreateResponse<ComposeExtensionResponse>(response)
                        : new HttpResponseMessage(HttpStatusCode.OK);

                }
                else if (activity.IsO365ConnectorCardActionQuery())
                {
                    // this will handle the request coming any action on Actionable messages
                    return await HandleO365ConnectorCardActionQuery(activity);
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }

        /// <summary>
        /// Handles O365 connector card action queries.
        /// </summary>
        /// <param name="activity">Incoming request from Bot Framework.</param>
        /// <param name="connectorClient">Connector client instance for posting to Bot Framework.</param>
        /// <returns>Task tracking operation.</returns>

        private static async Task<HttpResponseMessage> HandleO365ConnectorCardActionQuery(Activity activity)
        {
            var connectorClient = new ConnectorClient(new Uri(activity.ServiceUrl));

            // Get O365 connector card query data.
            O365ConnectorCardActionQuery o365CardQuery = activity.GetO365ConnectorCardActionQueryData();
            Activity replyActivity = activity.CreateReply();
            switch (o365CardQuery.ActionId)
            {
                case Constants.All:
                    await AttachAllPassengerList(replyActivity, "All passengers");
                    break;
                case Constants.ClassWise:
                    var classInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<O365BodyValue>(o365CardQuery.Body);
                    await AttachClassWisePassengerList(classInfo.Value, replyActivity, $"Passengers with {classInfo.Value} tickets");
                    break;
                case Constants.Zone:
                    var zoneInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<O365BodyValue>(o365CardQuery.Body);
                    await AttachZoneWisePassengerList(zoneInfo.Value, replyActivity, $"Passengers who belongs to zone {zoneInfo.Value}");
                    break;
                case Constants.FrequentFlyer:
                    await AttachFrequentFlyerPassengerList(replyActivity, "Frequent flyer passengers");
                    break;
                case Constants.SeatNumber:
                    var seatNo = Newtonsoft.Json.JsonConvert.DeserializeObject<O365BodyValue>(o365CardQuery.Body);
                    await AttachPassengerOnSpecifiedSeat(seatNo.Value, replyActivity);
                    break;
                case Constants.SpecialAssistance:
                    await AttachPassengerWhoNeedSpecialAssistance(replyActivity, "Passengers who need special assistance");
                    break;
                default:
                    break;
            }

            await connectorClient.Conversations.ReplyToActivityWithRetriesAsync(replyActivity);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        private static async Task AttachPassengerWhoNeedSpecialAssistance(Activity replyActivity, string title)
        {
            var passengers = await DocumentDBRepository<Passenger>.GetItemsAsync(d => d.SpecialAssistance != null && d.SpecialAssistance != string.Empty);
            AddPassengersInReply(replyActivity, passengers, title);
        }

        private static async Task AttachPassengerOnSpecifiedSeat(string seatNo, Activity replyActivity)
        {
            var passengers = await DocumentDBRepository<Passenger>.GetItemsAsync(d => d.Seat == seatNo);
            if (passengers.Count() == 0)
            {
                replyActivity.Text = $"Passenger with seat number {seatNo} does not exist.";
            }
            else
            {
                var replyCard = O365CardHelper.GetO365ConnectorCard(passengers.FirstOrDefault());
                replyActivity.Attachments.Add(replyCard.ToAttachment());
            }
        }

        private static async Task AttachFrequentFlyerPassengerList(Activity replyActivity, string title)
        {
            var passengers = await DocumentDBRepository<Passenger>.GetItemsAsync(d => d.FrequentFlyerNumber != null && d.FrequentFlyerNumber != string.Empty);
            AddPassengersInReply(replyActivity, passengers, title);
        }

        private static async Task AttachZoneWisePassengerList(string zoneInfo, Activity replyActivity, string title)
        {
            var passengers = await DocumentDBRepository<Passenger>.GetItemsAsync(d => d.Zone.ToLower() == zoneInfo.ToLower());
            AddPassengersInReply(replyActivity, passengers, title);
        }


        private static async Task AttachClassWisePassengerList(string classInfo, Activity replyActivity, string title)
        {
            var passengers = await DocumentDBRepository<Passenger>.GetItemsAsync(d => d.Class.ToLower() == classInfo.ToLower());
            AddPassengersInReply(replyActivity, passengers, title);
        }

        private static async Task AttachAllPassengerList(Activity replyActivity, string title)
        {
            var passengers = await DocumentDBRepository<Passenger>.GetItemsAsync(d => d != null);
            AddPassengersInReply(replyActivity, passengers, title);
        }

        private static void AddPassengersInReply(Activity replyActivity, System.Collections.Generic.IEnumerable<Passenger> passengers, string title)
        {
            var card = O365CardHelper.GetListCardAttachment(passengers, title + $" - {passengers.Count()}");
            try
            {
                replyActivity.Attachments.Add(card);
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);
            }
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }

    }
}
