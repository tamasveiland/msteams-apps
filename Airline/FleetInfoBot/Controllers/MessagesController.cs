using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using System.Collections.Generic;
using Airline.FleetInfoBot.Web.Model;
using Airline.FleetInfoBot.Web.Helper;
using System.Linq;
using Airline.FleetInfoBot.Web.Repository;
using System.Configuration;

namespace Airline.FleetInfoBot.Web.Controllers
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        [HttpPost]
        public async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            using (var connector = new ConnectorClient(new Uri(activity.ServiceUrl)))
            {
                if (activity != null && activity.GetActivityType() == ActivityTypes.Message)
                {
                    //CreateDataRecords();
                    await Conversation.SendAsync(activity, () => new EchoBot());
                }
                else if (activity.Type == ActivityTypes.Invoke)
                {
                    if (activity.IsComposeExtensionQuery())
                    {
                        var response = MessageExtension.HandleMessageExtensionQuery(connector, activity);
                        return response != null
                            ? Request.CreateResponse<ComposeExtensionResponse>(response)
                            : new HttpResponseMessage(HttpStatusCode.OK);
                    }
                    else if (activity.IsO365ConnectorCardActionQuery())
                    {
                        
                        return await HandleO365ConnectorCardActionQuery(activity);
                    }
                }
                return new HttpResponseMessage(HttpStatusCode.Accepted);
            }

        }

        private static async Task<HttpResponseMessage> HandleO365ConnectorCardActionQuery(Activity activity)
        {
            var connectorClient = new ConnectorClient(new Uri(activity.ServiceUrl));
            O365ConnectorCardActionQuery o365CardQuery = activity.GetO365ConnectorCardActionQueryData();
            Activity replyActivity = activity.CreateReply();
            try
            {
                if (o365CardQuery.ActionId == Constants.ShowAirCraftDetails)
                {
                    AirCraftDetails aircraftInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<AirCraftDetails>(o365CardQuery.Body);
                    await ShowAircraftDetails(aircraftInfo, replyActivity);
                    
                    ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                    await connector.Conversations.ReplyToActivityAsync(replyActivity);
                }
                else
                    await Conversation.SendAsync(activity, () => new EchoBot());
            }
            catch (Exception e)
            {
                activity.CreateReply(e.Message.ToString());
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
        private static async Task ShowAircraftDetails(AirCraftDetails aircraft, Activity replyActivity)
        {
            

            var list = await DocumentDBRepository<AirCraftInfo>.GetItemsAsync(d => d.FlightNumber == aircraft.FlightNumber && d.BaseLocation.ToLower() == aircraft.BaseLocation.ToLower());/* && d.JourneyDate.ToShortDateString()==flighinput.JourneyDate.ToShortDateString());*/
            if (list.Count() > 0)
            {
                var aircraftModels = list.FirstOrDefault();

                var aircraftDetails = await DocumentDBRepository<AirCraftInfo>.GetItemsAsync(d => d.FlightType.ToLower() == aircraftModels.FlightType.ToLower() && d.BaseLocation.ToLower() == aircraft.BaseLocation.ToLower());/* && d.JourneyDate.ToShortDateString()==flighinput.JourneyDate.ToShortDateString());*/
                if (aircraftDetails.Count() > 0)
                {
                    var ListCard = AddListCardAttachment(replyActivity, aircraftDetails);
                }
                else
                {
                    replyActivity.Text = $"Aircraft not available for selected base location and flight number.";
                }
            }
            else
            {
                replyActivity.Text = $"Aircraft not available for selected base location and flight number.";
            }
        }

        private static async Task AddListCardAttachment(Activity replyActivity, System.Collections.Generic.IEnumerable<AirCraftInfo> aircraftDetails)
        {
            var card = O365CardHelper.GetListofFlights(aircraftDetails);
            try
            {
                replyActivity.Attachments.Add(card);
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);
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
                    
                    replyActivity.Text = $"Aircraft {aircardInfo.AircraftId} has been assigned to flight: {aircardInfo.FlightNumber}.";
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
                    replyActivity.Text = $"Aircraft {aircardInfo.AircraftId} has been grounded.";
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
                    replyActivity.Text = $"Aircraft {aircardInfo.AircraftId} is available.";
                }
                catch (Exception e)
                {
                    replyActivity.Text = e.Message.ToString();
                }
            }

        }

        private static async Task CreateDataRecords()
        {
            AirCraftInfo obj1 = new AirCraftInfo();
            obj1.BaseLocation = "Seattle, WA";
            obj1.FlightNumber = "220";
            obj1.FlightType = "Small";
            obj1.Model = "Airbus A220";
            obj1.Capacity = "108-130";
            await DocumentDBRepository<AirCraftInfo>.CreateItemAsync(obj1);
            AirCraftInfo obj2 = new AirCraftInfo();
            obj2.BaseLocation = "Seattle, WA";
            obj2.FlightNumber = "320";
            obj2.FlightType = "Small";
            obj2.Model = "Airbus A320 family";
            obj2.Capacity = "107-206";
            await DocumentDBRepository<AirCraftInfo>.CreateItemAsync(obj2);
            AirCraftInfo obj3 = new AirCraftInfo();
            obj3.BaseLocation = "Seattle, WA";
            obj3.FlightNumber = "330";
            obj3.FlightType = "Medium";
            obj3.Model = "Airbus A330";
            obj3.Capacity = "247-287";
            await DocumentDBRepository<AirCraftInfo>.CreateItemAsync(obj3);
            AirCraftInfo obj4 = new AirCraftInfo();
            obj4.BaseLocation = "Seattle, WA";
            obj4.FlightNumber = "350";
            obj4.FlightType = "Medium";
            obj4.Model = "Airbus A350";
            obj4.Capacity = "276-366";
            await DocumentDBRepository<AirCraftInfo>.CreateItemAsync(obj4);
            AirCraftInfo obj5 = new AirCraftInfo();
            obj5.BaseLocation = "Seattle, WA";
            obj5.FlightNumber = "787";
            obj5.FlightType = "Medium";
            obj5.Model = "Boeing 787";
            obj5.Capacity = "242-330";
            await DocumentDBRepository<AirCraftInfo>.CreateItemAsync(obj5);
            AirCraftInfo obj6 = new AirCraftInfo();
            obj6.BaseLocation = "Seattle, WA";
            obj6.FlightNumber = "380";
            obj6.FlightType = "Large";
            obj6.Model = "Airbus A380";
            obj6.Capacity = "544";
            await DocumentDBRepository<AirCraftInfo>.CreateItemAsync(obj6);
            AirCraftInfo obj7 = new AirCraftInfo();
            obj7.BaseLocation = "Seattle, WA";
            obj7.FlightNumber = "777";
            obj7.FlightType = "Large";
            obj7.Model = "Boeing 777-200LR/300ER/Boeing 777X";
            obj7.Capacity = "350-400";
            await DocumentDBRepository<AirCraftInfo>.CreateItemAsync(obj7);
        }
    }
}
