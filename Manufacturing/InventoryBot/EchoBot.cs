using System;
using System.Collections.Generic;

using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using Manufacturing.InventoryInfoBot.Helper;
using Manufacturing.InventoryInfoBot.Repository;
using Manufacturing.InventoryInfoBot.Model;
using System.Linq;
using System.Configuration;

namespace Manufacturing.InventoryInfoBot
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
            

        }

        private async Task HandleMessage(IDialogContext context, IMessageActivity message)
        {
            
            if (message.Text != null && message.Text.Contains("Show details of product"))
            {
                
                var reply = (Activity)message;
                Activity replyActivity = reply.CreateReply();
                var actionId = Guid.NewGuid().ToString();
                var productid = System.Text.RegularExpressions.Regex.Match(message.Text, @"\(([^)]*)\)").Groups[1].Value;
                var list = await DocumentDBRepository<Product>.GetItemsAsync(d => d.PrdouctId == Convert.ToInt32(productid));
                var productResultCard = O365CardHelper.GetAdativeCard(list.FirstOrDefault(), actionId);
                replyActivity.Attachments.Add(productResultCard);
                try
                {
                    ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
                    var msgToUpdate = await connector.Conversations.ReplyToActivityAsync(replyActivity);
                    context.ConversationData.SetValue(actionId, msgToUpdate.Id);
                    privateStorage.Add(actionId, msgToUpdate.Id);
                    
                }
                catch(Exception e)
                {
                    await context.PostAsync((replyActivity));
                }
                
            }
            else if (message.Value != null)
            {
                var activity = (Activity)message;
                InventoryInputDetails itemCount = Newtonsoft.Json.JsonConvert.DeserializeObject<InventoryInputDetails>(activity.Value.ToString());
                
                var connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                Activity replyActivity = activity.CreateReply();
                switch (itemCount.Type)
                {
                    case Constants.newInventoryCount:
                        await AddItems(itemCount, replyActivity);

                        
                        break;
                    case Constants.BlockInventory:
                        await BlockItems(itemCount, replyActivity);
                        
                        break;
                    case Constants.RetireInventory:
                        await RetireItems(itemCount, replyActivity);
                        
                        break;
                    case Constants.RequestNewStock:
                        await AttachNewStock(replyActivity);
                        
                        await connector.Conversations.ReplyToActivityAsync(replyActivity);
                        break;

                    default:
                        break;
                }
                if (itemCount.Type != Constants.RequestNewStock)
                {
                    var lastMessageId = context.ConversationData.GetValueOrDefault<string>(itemCount.ActionId);
                    if (lastMessageId == null && privateStorage.ContainsKey(itemCount.ActionId))
                        lastMessageId = privateStorage[itemCount.ActionId];
                    if (!string.IsNullOrEmpty(lastMessageId))
                    {
                        // Update existing item.
                        await connector.Conversations.UpdateActivityAsync(replyActivity.Conversation.Id, lastMessageId, replyActivity);
                        context.ConversationData.RemoveValue(itemCount.ActionId);
                       
                    }
                    else
                    {
                        await connector.Conversations.SendToConversationAsync(replyActivity);
                    }
                }
                
            }
            else
            {
                var messageText = message.Text.ToLower();
                var reply = context.MakeMessage();
                List<Industry> lst = new List<Industry>();
                Industry obj = new Industry();
                obj.IndsutryCode = "air";
                obj.IndustryName = "Airlines";
                lst.Add(obj);
                Industry obj1 = new Industry();
                obj1.IndsutryCode = "ret";
                obj1.IndustryName = "Retail";
                lst.Add(obj1);
                Industry obj2 = new Industry();
                obj2.IndsutryCode = "mft";
                obj2.IndustryName = "Manufacturing";
                lst.Add(obj2);
                var industryNames = lst;
                reply.Attachments.Add(GetCardsInformation(industryNames));

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
            Activity replyActivity = activity.CreateReply();
            switch (o365CardQuery.ActionId)
            {
               case Constants.newInventoryCount:
                    InventoryInputDetails itemCount = Newtonsoft.Json.JsonConvert.DeserializeObject<InventoryInputDetails>(o365CardQuery.Body);
                    await AddItems(itemCount, replyActivity);
                    break;
                case Constants.BlockInventory:
                    InventoryInputDetails blockItem = Newtonsoft.Json.JsonConvert.DeserializeObject<InventoryInputDetails>(o365CardQuery.Body);
                    await BlockItems(blockItem, replyActivity);
                    break;
                case Constants.RetireInventory:
                    InventoryInputDetails retireitemcount = Newtonsoft.Json.JsonConvert.DeserializeObject<InventoryInputDetails>(o365CardQuery.Body);
                    await RetireItems(retireitemcount, replyActivity);
                    break;
                case Constants.RequestNewStock:
                    await AttachNewStock(replyActivity);
                    context.ConversationData.RemoveValue(LastMessageIdKey);
                    break;
                default:
                    break;

            }
            
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
        private static string savedMessageId;
        private static async Task AttachNewStock(Activity replyActivity)
        {
            replyActivity.Text = "Thanks for your request. The procurement team has been notified of your request";
        }

        private static async Task AddItems(InventoryInputDetails itemcount, Activity replyActivity)
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
                            if (loc.Quantity - loc.Committed > 0)
                            {
                                var itemsList = await DocumentDBRepository<Product>.UpdateItemAsync(list.Id, list);
                                var replyCard = O365CardHelper.GetAdativeCard(addItems.FirstOrDefault(), itemcount.ActionId);
                                replyActivity.Attachments.Add(replyCard);
                            }
                            else
                            {
                                replyActivity.Text = "Items are not availbile";
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

        public static Attachment GetCardsInformation(IEnumerable<Industry> industriesList)
        {

            var list = new List<O365ConnectorCardMultichoiceInputChoice>();
            foreach (var industry in industriesList)
            {
                list.Add(new O365ConnectorCardMultichoiceInputChoice(industry.IndustryName, industry.IndsutryCode));
            }


            var section = new O365ConnectorCardSection
            {
                ActivityTitle = "A peek into inventory across locations across products.",
                
            };
            
            
            var IndustryInfo = new O365ConnectorCardActionCard(
                O365ConnectorCardActionCard.Type,
                "Show Inventory",
                "Multiple Choice Card",
                new List<O365ConnectorCardInputBase>
                {
                    new O365ConnectorCardMultichoiceInput(
                        O365ConnectorCardMultichoiceInput.Type,
                        "Industry",
                        true,
                        "Industry Ex: Airline, Retail",
                        null,
                        list,
                        "compact",
                        false),
               },
               

            new List<O365ConnectorCardActionBase>
                  {
                   new O365ConnectorCardHttpPOST(
                        O365ConnectorCardHttpPOST.Type,
                        "Industry",
                        Constants.Industry,
                        @"{""Value"":""{{Industry.value}}""}")
                 });

            
            O365ConnectorCard card = new O365ConnectorCard()
            {
                ThemeColor = "#E67A9E",
                Title = "Inventory Information",
                Sections = new List<O365ConnectorCardSection> { section },
                PotentialAction = new List<O365ConnectorCardActionBase>
                {
                    IndustryInfo

                }
            };
            return card.ToAttachment();

        }
    }
}
