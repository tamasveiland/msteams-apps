using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using Airline.BaggageInfoBot.Web.Model;
using Airline.BaggageInfoBot.Web.Helper;
using System.Linq;
using System.Collections.Generic;
using Airline.BaggageInfoBot.Web.Repository;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Airline.BaggageInfoBot.Web.Controllers
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
                        //CreateDataRecords();
                        return await HandleO365ConnectorCardActionQuery(activity);
                    }
                }
                else
                {
                    HandleSystemMessage(activity);
                }
               
                return new HttpResponseMessage(HttpStatusCode.Accepted);
            }
        }

        private static async Task<HttpResponseMessage> HandleO365ConnectorCardActionQuery(Activity activity)
        {
            var connectorClient = new ConnectorClient(new Uri(activity.ServiceUrl));
            
            // Get O365 connector card query data.
            O365ConnectorCardActionQuery o365CardQuery = activity.GetO365ConnectorCardActionQueryData();
            Activity replyActivity = activity.CreateReply();
            try
            {
                if (o365CardQuery.ActionId == Constants.Name)
                {
                    var Name = Newtonsoft.Json.JsonConvert.DeserializeObject<O365BodyValue>(o365CardQuery.Body);
                    await AttachBaggageInformationName(Name.Value, replyActivity);
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
        
        private static async Task AttachRebookInformation(string FlightNumber, Activity replyActivity)
        {

            replyActivity.Text = "Your Baggage is rebooked with Flight Number: " + FlightNumber;
        }
        private static async Task AttachBaggagebyPNR(string PNR,Activity replyActivity)
        {
            
            var list = await DocumentDBRepository<Baggage>.GetItemsAsync(d => d.PNR.ToLower() == PNR.ToLower());
            if (list.Count() == 0)
            {
                replyActivity.Text = "The passenger with PNR " + PNR + " does not have any checked baggage.";
                
            }
            else
            {
                var BaggagebyPNR = O365CardHelper.GetO365ConnectorCard(list.FirstOrDefault());
                replyActivity.Attachments.Add(BaggagebyPNR.ToAttachment());
            }
        }

        private static async Task CreateDataRecords()
        {
            
            Baggage obj2 = new Baggage();
            obj2.BagCount = 2;
            obj2.CurrentStatus = "Boston, MA";
            obj2.FlightNumber = "475";
            obj2.From = "Boston, MA";
            obj2.To = "Washington, DC";
            obj2.Gender = "Female";
            obj2.Name = "Ashley Solis";
            obj2.PNR = "DBW6W1";
            obj2.SeatNo = "27L";
            obj2.TicketNo = "DBW6KK";
            await DocumentDBRepository<Baggage>.CreateItemAsync(obj2);
            Baggage obj5 = new Baggage();
            obj5.BagCount = 2;
            obj5.CurrentStatus = "Boston, MA";
            obj5.FlightNumber = "475";
            obj5.From = "Boston, MA";
            obj5.To = "Washington, DC";
            obj5.Gender = "Female";
            obj5.Name = "Rebecca Larson";
            obj5.PNR = "DBW6W2";
            obj5.SeatNo = "27G";
            obj5.TicketNo = "DBW6KG";
            await DocumentDBRepository<Baggage>.CreateItemAsync(obj5);
            Baggage obj3 = new Baggage();
            obj3.BagCount = 2;
            obj3.CurrentStatus = "Washington, DC";
            obj3.FlightNumber = "476";
            obj3.From = "Boston, MA";
            obj3.To = "Washington, DC";
            obj3.Gender = "Male";
            obj3.Name = "Galen O'Shea";
            obj3.PNR = "DBW6W3";
            obj3.SeatNo = "27A";
            obj3.TicketNo = "DBW6W2";
            await DocumentDBRepository<Baggage>.CreateItemAsync(obj3);
            Baggage obj4 = new Baggage();
            obj4.BagCount = 2;
            obj4.CurrentStatus = "Washington, DC";
            obj4.FlightNumber = "478";
            obj4.From = "Boston, MA";
            obj4.To = "Washington, DC";
            obj4.Gender = "Male";
            obj4.Name = "Ruben Comeaux";
            obj4.PNR = "DBW6W4";
            obj4.SeatNo = "27C";
            obj4.TicketNo = "DBW6W4";
            await DocumentDBRepository<Baggage>.CreateItemAsync(obj4);

            Baggage obj6 = new Baggage();
            obj6.BagCount = 2;
            obj6.CurrentStatus = "Washington, DC";
            obj6.FlightNumber = "479";
            obj6.From = "Boston, MA";
            obj6.To = "Washington, DC";
            obj6.Gender = "Male";
            obj6.Name = "Don Howes";
            obj6.PNR = "DBW6W5";
            obj6.SeatNo = "27D";
            obj6.TicketNo = "DBW6W7";
            await DocumentDBRepository<Baggage>.CreateItemAsync(obj6);

        }

        private static async Task AttachBaggageInformationTicket(string Ticket, Activity replyActivity)
        {
          
            var list = await DocumentDBRepository<Baggage>.GetItemsAsync(d => d.TicketNo.ToLower() == Ticket.ToLower());
            if (list.Count() == 0)
            {
                replyActivity.Text = "The passenger with ticket " + Ticket + " does not have any checked baggage.";
            }
            else
            {
                var replyCard = O365CardHelper.GetO365ConnectorCard(list.FirstOrDefault());
                replyActivity.Attachments.Add(replyCard.ToAttachment());
            }
        }

        private static async Task AttachBaggageInformationName(string Name, Activity replyActivity)
        {


            // var list = listobj.Where(l => l.Name.Contains(Name));
            var actionId = Guid.NewGuid().ToString();
            var list = await DocumentDBRepository<Baggage>.GetItemsAsync(d => d.Name.ToLower().Contains(Name.ToLower()));
            int count = list.Count();
            if (list.Count()>1)
            {
                var ListCard = AddListCardAttachment(replyActivity, list);
            }
            else if(list.Count()==1)
            {
                var replyCard = O365CardHelper.GetO365ConnectorCard(list.FirstOrDefault());
                replyActivity.Attachments.Add(replyCard.ToAttachment());
            }
            else
            {
                replyActivity.Text=$"Passenger with Name {Name} does not exist.";
            }
            
            
        }

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

        private static async Task AttachBaggageInformation(string PNR, Activity replyActivity)
        {

            var list = await DocumentDBRepository<Baggage>.GetItemsAsync(d => d.PNR == PNR);
            if (list.Count() == 0)
            {
                replyActivity.Text = "Baggage with PNR " + PNR +  " not found in the system.";
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
            replyActivity.Text = "Report registered. Reference ticket number is: "+ TicketNumberref + " The passenger has been notified using the contact number provided with the booking.";
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
