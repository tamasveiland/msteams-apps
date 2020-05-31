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
using Manufacturing.InventoryInfoBot.Model;
using Manufacturing.InventoryInfoBot.Helper;
using System.Linq;
using Manufacturing.InventoryInfoBot.Repository;

namespace Manufacturing.InventoryInfoBot.Controllers
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
                        
                        return await HandleO365ConnectorCardActionQuery(activity);
                    }
                }
                else if (activity.Type == ActivityTypes.Message)
                {
                    ConnectorClient connector1 = new ConnectorClient(new Uri(activity.ServiceUrl));
                    Activity reply = activity.CreateReply($"You sent {activity.Text} which was {activity.Text.Length} characters.");

                    var msgToUpdate = await connector.Conversations.ReplyToActivityAsync(reply);
                    Activity updatedReply = activity.CreateReply($"This is an updated message.");
                    await connector.Conversations.UpdateActivityAsync(reply.Conversation.Id, msgToUpdate.Id, updatedReply);
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
                if (o365CardQuery.ActionId == Constants.Industry)
                {
                    var industryCode = Newtonsoft.Json.JsonConvert.DeserializeObject<O365BodyValue>(o365CardQuery.Body);
                    await ShowProductInfo(industryCode.Value, replyActivity);

                    ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                    await connector.Conversations.ReplyToActivityAsync(replyActivity);
                }
                
            }
            catch(Exception e)
            {
                activity.CreateReply(e.Message.ToString());
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
        private static async Task ShowProductInfo(string IndustryCode, Activity replyActivity)
        {
            
            var list = await DocumentDBRepository<Product>.GetItemsAsync(d => d.IndustryCode.ToLower()==IndustryCode.ToLower() && d.IsActive==true);/* && d.JourneyDate.ToShortDateString()==flighinput.JourneyDate.ToShortDateString());*/

            
            if (list.Count() > 0)
            {
                var ListCard = AddListCardAttachment(replyActivity, list);
            }
            else
            {
                replyActivity.Text = $"Products not avilibile for selected industry.";
            }
        }
        private static async Task AttachNewStock(Activity replyActivity)
        {
            replyActivity.Text = "Thanks for your request. The procurement team has been notified of your request." ;
        }

        private static async Task AddItems(InventoryInputDetails itemcount, Activity replyActivity)
        {
            var addItems = await DocumentDBRepository<Product>.GetItemsAsync(d => d.PrdouctId == Convert.ToInt32(itemcount.ProductId));
            if (addItems.Count() > 0)
            {
                try
                {
                    var list = addItems.FirstOrDefault();
                    foreach(var loc in list.locationList)
                    {
                        if(itemcount.Location.ToLower()==loc.Location.ToLower())
                        {
                            loc.Quantity = Convert.ToInt32(loc.Quantity) + Convert.ToInt32(itemcount.newItemCount);
                        }
                    }
                    
                    var itemsList = await DocumentDBRepository<Product>.UpdateItemAsync(list.Id, list);
                    var replyCard = O365CardHelper.GetAdativeCard(addItems.FirstOrDefault(), itemcount.ActionId);
                    replyActivity.Attachments.Add(replyCard);
                    
                }
                catch (Exception e)
                {
                    replyActivity.Text = e.Message.ToString();
                }
            }

        }

        private static async Task RetireItems(InventoryInputDetails itemcount, Activity replyActivity)
        {
            var addItems = await DocumentDBRepository<Product>.GetItemsAsync(d => d.PrdouctId == Convert.ToInt32(itemcount.ProductId));
            if (addItems.Count() > 0)
            {
                try
                {
                    var list = addItems.FirstOrDefault();
                    foreach (var loc in list.locationList)
                    {
                        if (itemcount.Location.ToLower() == loc.Location.ToLower())
                        {
                            loc.Quantity = Convert.ToInt32(loc.Quantity) - Convert.ToInt32(itemcount.newItemCount);
                        }
                    }
                    
                    var itemsList = await DocumentDBRepository<Product>.UpdateItemAsync(list.Id, list);
                    var replyCard = O365CardHelper.GetAdativeCard(addItems.FirstOrDefault(), itemcount.ActionId);
                    replyActivity.Attachments.Add(replyCard);
                    
                }
                catch (Exception e)
                {
                    replyActivity.Text = e.Message.ToString();
                }
            }

        }

        private static async Task BlockItems(InventoryInputDetails itemcount, Activity replyActivity)
        {
            var addItems = await DocumentDBRepository<Product>.GetItemsAsync(d => d.PrdouctId == Convert.ToInt32(itemcount.ProductId));
            if (addItems.Count() > 0)
            {
                try
                {
                    var list = addItems.FirstOrDefault();
                    foreach (var loc in list.locationList)
                    {
                        if (itemcount.Location.ToLower() == loc.Location.ToLower())
                        {
                            loc.Committed = Convert.ToInt32(loc.Committed) + Convert.ToInt32(itemcount.newItemCount);
                            if(loc.Quantity-loc.Committed>0)
                            {
                                var itemsList = await DocumentDBRepository<Product>.UpdateItemAsync(list.Id, list);
                                var replyCard = O365CardHelper.GetAdativeCard(addItems.FirstOrDefault(), itemcount.ActionId);
                                replyActivity.Attachments.Add(replyCard);
                            }
                            else
                            {
                                replyActivity.Text = "Items are not available.";
                            }
                        }
                    }
                 
                }
                catch (Exception e)
                {
                    replyActivity.Text = e.Message.ToString();
                }
            }

        }

       
        
        private static async Task AddListCardAttachment(Activity replyActivity, System.Collections.Generic.IEnumerable<Product> productInfo)
        {
            var card = O365CardHelper.GetListofProducts(productInfo);
            try
            {
                replyActivity.Attachments.Add(card);
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);
            }
        }

        private static async Task AttachRebookPassenger(string flightNumber, Activity replyActivity)
        {
            replyActivity.Text = $"Passenger has been rebooked on flight number: " + flightNumber;
        }

        private static async Task CreateDataRecords()
        {
            List<Product> lst = new List<Product>();
            Product obj5 = new Product();
            obj5.PrdouctId = 10;
            obj5.ProductName = "Casters";
            obj5.IsActive = true;
            //obj5.Quantity = 10;
            obj5.IndustryCode = "mft";
            //obj5.Location = "Hyderbad";

            List<Locationbased> lst1 = new List<Locationbased>();
            Locationbased obj = new Locationbased();
            obj.Location = "Hyderbad";
            obj.Quantity = 10;
            lst1.Add(obj);
            Locationbased obj12 = new Locationbased();
            obj12.Location = "Banaglore";
            obj12.Quantity = 10;
            lst1.Add(obj12);
            obj5.locationList = lst1;
            await DocumentDBRepository<Product>.CreateItemAsync(obj5);
        }



    }
}
