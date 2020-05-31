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
using Airline.FlightInfoBot.Web.Model;
using Airline.FlightInfoBot.Web.Helper;
using System.Linq;
using Airline.FlightInfoBot.Web.Repository;

namespace Airline.FlightInfoBot.Web.Controllers
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
            switch (o365CardQuery.ActionId)
            {
                case Constants.ShowFlights:
                    FlightInputDetails flightInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<FlightInputDetails>(o365CardQuery.Body);
                    flightInfo.JourneyDate = flightInfo.JourneyDate + activity.LocalTimestamp.Value.Offset;
                    await ShowFlightInfo(flightInfo, replyActivity);
                    break;
                case Constants.Rebook:
                    RebookClass rebookFlight = Newtonsoft.Json.JsonConvert.DeserializeObject<RebookClass>(o365CardQuery.Body);
                    await AttachRebookPassenger(rebookFlight.flightNumberInput, rebookFlight.pnrNumberInput, replyActivity);
                    break;
                default:
                    break;
            }
            await connectorClient.Conversations.ReplyToActivityWithRetriesAsync(replyActivity);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
        private static async Task ShowFlightInfo(FlightInputDetails flighinput, Activity replyActivity)
        {
            //DateTime local = flighinput.JourneyDate.ToUniversalTime();

            var list = await DocumentDBRepository<FlightInfo>.GetItemsAsync(d => d.FromCity == flighinput.From && d.ToCity == flighinput.To);/* && d.JourneyDate.ToShortDateString()==flighinput.JourneyDate.ToShortDateString());*/

            
            if (list.Count() > 0)
            {
                var ListCard = AddListCardAttachment(replyActivity, list, flighinput.JourneyDate.ToUniversalTime().Date);
            }
            else
            {
                replyActivity.Text = $"Flights are not available for selected date. Please check another date.";
            }
        }

        private static async Task AddListCardAttachment(Activity replyActivity, System.Collections.Generic.IEnumerable<FlightInfo> flightinfo, DateTime Journetdate)
        {
            var card = O365CardHelper.GetListofFlights(flightinfo, Journetdate);
            try
            {
                replyActivity.Attachments.Add(card);
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);
            }
        }

        private static async Task AttachRebookPassenger(string flightnumber, string Pnrnumber, Activity replyActivity)
        {
            replyActivity.Text = $"Passenger with PNR number: {Pnrnumber} has been rebooked on flight number: {flightnumber}";
        }

        private static async Task CreateDataRecords()
        {
            FlightInfo obj1 = new FlightInfo();
            obj1.FlightNumber = "475";
            obj1.FlightName = "Constoso Airline";
            obj1.JourneyDate = DateTime.UtcNow.AddDays(4);
            obj1.SeatCount = 10;
            obj1.Status = "Ready to Fly";
            obj1.Type = "Flight";
            obj1.FromCity = "SEA";
            obj1.ToCity = "BWI";
            obj1.Departure = DateTime.UtcNow.AddDays(4); 
            obj1.Arrival = DateTime.UtcNow.AddDays(4);
            obj1.PNR = "DBW6WL";

            await DocumentDBRepository<FlightInfo>.CreateItemAsync(obj1);

            FlightInfo obj2 = new FlightInfo();
            obj2.FlightNumber = "475";
            obj2.FlightName = "Constoso Airline";
            obj2.JourneyDate = DateTime.UtcNow.AddDays(4);
            obj2.SeatCount = 10;
            obj2.Status = "Ready to Fly";
            obj2.Type = "Flight";
            obj2.FromCity = "BWI";
            obj2.ToCity = "ORD";
            obj2.Departure = DateTime.UtcNow.AddDays(4);
            obj2.Arrival = DateTime.UtcNow.AddDays(4);
            obj2.PNR = "DBW6WK";

            await DocumentDBRepository<FlightInfo>.CreateItemAsync(obj2);

            FlightInfo obj3= new FlightInfo();
            obj3.FlightNumber = "475";
            obj3.FlightName = "Constoso Airline";
            obj3.JourneyDate = DateTime.UtcNow.AddDays(6);
            obj3.SeatCount = 10;
            obj3.Status = "Ready to Fly";
            obj3.Type = "Flight";
            obj3.FromCity = "BWI";
            obj3.ToCity = "ORD";
            obj3.Departure = DateTime.UtcNow.AddDays(6);
            obj3.Arrival = DateTime.UtcNow.AddDays(6);
            obj3.PNR = "DBW6WJ";
        
            await DocumentDBRepository<FlightInfo>.CreateItemAsync(obj3);

            FlightInfo obj4 = new FlightInfo();
            obj4.FlightNumber = "475";
            obj4.FlightName = "Constoso Airline";
            obj4.JourneyDate = DateTime.UtcNow.AddDays(6);
            obj4.SeatCount = 50;
            obj4.Status = "Ready to Fly";
            obj4.Type = "Flight";
            obj4.FromCity = "JFK";
            obj4.ToCity = "SEA";
            obj4.Departure = DateTime.UtcNow.AddDays(6);
            obj4.Arrival = DateTime.UtcNow.AddDays(6);
            obj4.PNR = "DBW6WG";

            await DocumentDBRepository<FlightInfo>.CreateItemAsync(obj4);


            FlightInfo obj5 = new FlightInfo();
            obj5.FlightNumber = "475";
            obj5.FlightName = "Constoso Airline";
            obj5.JourneyDate = DateTime.UtcNow.AddDays(6);
            obj5.SeatCount = 10;
            obj5.Status = "Ready to Fly";
            obj5.Type = "Flight";
            obj5.FromCity = "SEA";
            obj5.ToCity = "JFK";
            obj5.Departure = DateTime.UtcNow.AddDays(6);
            obj5.Arrival = DateTime.UtcNow.AddDays(6);
            obj5.PNR = "DBW6WG";

            await DocumentDBRepository<FlightInfo>.CreateItemAsync(obj5);
        }
    }
}
