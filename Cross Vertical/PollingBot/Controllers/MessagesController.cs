using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using CrossVertical.PollingBot.Models;
using CrossVertical.PollingBot.Repository;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
namespace CrossVertical.PollingBot.Controllers
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        [HttpPost]
        public async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            if (activity != null && activity.Type == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new EchoBot());
                return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
            }
            else
            {
                await HandleSystemMessage(activity);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }

        private async Task<string> GetUserEmailId(string userId, string serviceUrl, string TeamorConversationId)
        {
            // Fetch the members in the current conversation
            ConnectorClient connector = new ConnectorClient(new Uri(serviceUrl));
            var members = await connector.Conversations.GetConversationMembersAsync(TeamorConversationId);
            return members.Where(m => m.Id == userId).First().AsTeamsChannelAccount().UserPrincipalName;

        }
        private async Task<string> GetUserName(string userId, string serviceUrl, string TeamorConversationId)
        {
            // Fetch the members in the current conversation
            ConnectorClient connector = new ConnectorClient(new Uri(serviceUrl));
            var members = await connector.Conversations.GetConversationMembersAsync(TeamorConversationId);
            return members.Where(m => m.Id == userId).First().AsTeamsChannelAccount().Name;

        }

        private async Task<Activity> HandleSystemMessage(Activity message)
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

                if (message.MembersAdded.Any(m => m.Id.Contains(message.Recipient.Id)))
                {
                    try
                    {
                        var connectorClient = new ConnectorClient(new Uri(message.ServiceUrl));
                        var channelData = message.GetChannelData<TeamsChannelData>();
                        var TeamorConversationId = channelData.Team != null ? channelData.Team.Id : message.Conversation.Id;
                        if (channelData.Team == null)
                        {
                            await AddtoDatabase(message, TeamorConversationId, message.From.Id);
                            //await Conversation.SendAsync(message, () => new EchoBot());
                            ThumbnailCard card = EchoBot.GetWelcomeMessage();
                            var reply = message.CreateReply();
                            reply.TextFormat = TextFormatTypes.Xml;
                            reply.Attachments.Add(card.ToAttachment());
                            await connectorClient.Conversations.ReplyToActivityAsync(reply);
                            return null;

                            
                        }

                        var members = await connectorClient.Conversations.GetConversationMembersAsync(TeamorConversationId);
                        foreach (var meb in members)
                        {
                            await AddtoDatabase(message, TeamorConversationId, meb.Id);
                            ThumbnailCard card = EchoBot.GetWelcomeMessage();
                            //ThumbnailCard card = EchoBot.GetHelpMessage();
                            var replyMessage = Activity.CreateMessageActivity();
                            var parameters = new ConversationParameters
                            {
                                Members = new ChannelAccount[] { new ChannelAccount(meb.Id) },
                                ChannelData = new TeamsChannelData
                                {
                                    Tenant = channelData.Tenant,
                                    Notification = new NotificationInfo() { Alert = true }
                                }
                            };

                            var conversationResource = await connectorClient.Conversations.CreateConversationAsync(parameters);
                            replyMessage.ChannelData = new TeamsChannelData() { Notification = new NotificationInfo(true) };
                            replyMessage.Conversation = new ConversationAccount(id: conversationResource.Id.ToString());
                            replyMessage.TextFormat = TextFormatTypes.Xml;
                            replyMessage.Attachments.Add(card.ToAttachment());
                            await connectorClient.Conversations.SendToConversationAsync((Activity)replyMessage);


                        }
                        return null;
                    }
                    catch (Exception ex)
                    {

                        return null;
                    }
                    // Bot Installation
                    // Bot is added. Let's send welcome message.
                    //var connectorClient = new ConnectorClient(new Uri(message.ServiceUrl));

                }

                // For Add new member
                for (int i = 0; i < message.MembersAdded.Count; i++)
                {
                    if (message.MembersAdded[i].Id != message.Recipient.Id)
                    {
                        try
                        {
                            var connectorClient = new ConnectorClient(new Uri(message.ServiceUrl));
                            var userId = message.MembersAdded[i].Id;
                            var channelData = message.GetChannelData<TeamsChannelData>();
                            var user = new UserDetails();
                            var TeamorConversationId = channelData.Team != null ? channelData.Team.Id : message.Conversation.Id;
                            //string emailid = await GetUserEmailId(userId, message.ServiceUrl, TeamorConversationId);
                            //user.EmaildId = emailid;
                            user.EmaildId = await GetUserEmailId(userId, message.ServiceUrl, TeamorConversationId);

                            user.UserId = userId;
                            user.UserName = await GetUserName(userId, message.ServiceUrl, TeamorConversationId);
                            var parameters = new ConversationParameters
                            {
                                Members = new ChannelAccount[] { new ChannelAccount(userId) },
                                ChannelData = new TeamsChannelData
                                {
                                    Tenant = channelData.Tenant,
                                    Notification = new NotificationInfo() { Alert = true }
                                }
                            };

                            var conversationResource = await connectorClient.Conversations.CreateConversationAsync(parameters);

                            var replyMessage = Activity.CreateMessageActivity();
                            replyMessage.ChannelData = new TeamsChannelData() { Notification = new NotificationInfo(true) };
                            replyMessage.Conversation = new ConversationAccount(id: conversationResource.Id.ToString());
                            var name = await GetUserName(userId, message.ServiceUrl, TeamorConversationId);
                            if (name != null)
                            {
                                name = name.Split(' ').First();
                            }
                            user.Type = Helper.Constants.NewUser;
                            var existinguserRecord = await DocumentDBRepository.GetItemsAsync<UserDetails>(u => u.EmaildId == user.EmaildId && u.Type == Helper.Constants.NewUser);
                            if (existinguserRecord.Count() == 0)
                            {
                                var NewUserRecord = await DocumentDBRepository.CreateItemAsync(user);
                            }
                            ThumbnailCard card = EchoBot.GetWelcomeMessage();
                            replyMessage.Attachments.Add(card.ToAttachment());

                            await connectorClient.Conversations.SendToConversationAsync((Activity)replyMessage);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }

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

        private async Task AddtoDatabase(Activity message, string TeamorConversationId, string mebId)
        {
            var user = new UserDetails();
            //var TeamorConversationId = channelData.Team != null ? channelData.Team.Id : message.Conversation.Id;
            user.EmaildId = await GetUserEmailId(mebId, message.ServiceUrl, TeamorConversationId);
            user.UserId = mebId;
            user.UserName = await GetUserName(mebId, message.ServiceUrl, TeamorConversationId);
            if (user.UserName != null)
            {
                user.UserName = user.UserName.Split(' ').FirstOrDefault();
            }
            user.Type = Helper.Constants.NewUser;
            var existinguserRecord = await DocumentDBRepository.GetItemsAsync<UserDetails>(u => u.EmaildId == user.EmaildId && u.Type == Helper.Constants.NewUser);
            if (existinguserRecord.Count() == 0)
            {
                var NewUserRecord = await DocumentDBRepository.CreateItemAsync(user);
            }
        }
    }
}
