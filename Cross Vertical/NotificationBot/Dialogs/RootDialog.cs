using AdaptiveCards;
using CrossVertical.NotificationBot.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CrossVertical.NotificationBot
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private static Dictionary<string, string> privateStorage = new Dictionary<string, string>();

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = (Activity)await argument;
            var messageText = message.GetTextWithoutMentions();
            if (messageText == "ConnectorAction")
            {
                await HandleConnectorAction(context, message);
                // context.ConversationData.SetValue(actionId, msgToUpdate.Id);
            }
            else
            {
                ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
                var channelData = message.GetChannelData<TeamsChannelData>();
                IList<ChannelAccount> members = await GetAllMembers(connector, channelData);

                Activity reply = message.CreateReply();
                var actionId = Guid.NewGuid().ToString();
                reply.Attachments.Add(GetWelcomeMessage(actionId, members));

                if (members != null)
                    context.ConversationData.SetValue(channelData.Channel.Id, members.Select(m => m.Id).ToList());

                var msgToUpdate = await connector.Conversations.ReplyToActivityAsync(reply);
                context.ConversationData.SetValue(actionId, msgToUpdate.Id);
                privateStorage.Add(actionId, msgToUpdate.Id);
            }

            context.Wait(MessageReceivedAsync);
        }

        private static async Task<IList<ChannelAccount>> GetAllMembers(ConnectorClient connector, TeamsChannelData channelData)
        {
            IList<ChannelAccount> members = null;
            if (channelData.Team != null)
            {
                members = await connector.Conversations.GetConversationMembersAsync(channelData.Team.Id);
            }

            return members;
        }

        private async Task HandleConnectorAction(IDialogContext context, Activity message)
        {
            O365ConnectorCardActionQuery o365CardQuery = message.GetO365ConnectorCardActionQueryData();
            O365ConnectorActionRequest actionInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<O365ConnectorActionRequest>(o365CardQuery.Body);

            Activity reply = message.CreateReply();

            switch (actionInfo.Value)
            {
                case "Weather":
                    reply.Attachments.Add(GetWeatherCard());
                    break;
                case "OperationsDelay":
                    reply.Attachments.Add(GetOperationsDelayCard());
                    break;
                case "SocialEvents":
                    List<Attachment> events = GetSocialEvents();
                    foreach (var evnt in events)
                    {
                        reply.Attachments.Add(evnt);
                    }
                    reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    break;
                default:
                    break;
            }

            MicrosoftAppCredentials.TrustServiceUrl(message.ServiceUrl, DateTime.MaxValue);
            ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));

            reply.ChannelData = new TeamsChannelData() { Notification = new NotificationInfo(true) };

            var lastMessageId = context.ConversationData.GetValueOrDefault<string>(actionInfo.ActionId);
            if (lastMessageId == null && privateStorage.ContainsKey(actionInfo.ActionId))
                lastMessageId = privateStorage[actionInfo.ActionId];
            if (!string.IsNullOrEmpty(lastMessageId))
            {
                // Update existing item.
                await connector.Conversations.UpdateActivityAsync(reply.Conversation.Id, lastMessageId, reply);
                context.ConversationData.RemoveValue(actionInfo.ActionId);
                if (privateStorage.ContainsKey(actionInfo.ActionId))
                    privateStorage.Remove(actionInfo.ActionId);

                // Send private messages.
                if (!string.IsNullOrEmpty(actionInfo.Members))
                {
                    // Send private message to these users.
                    var members = actionInfo.Members.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (members.Any(x => x.Contains("ALL TEAM")))
                    {
                        // Provide option to send message to all members.
                        var channelData = message.GetChannelData<TeamsChannelData>();
                        if (context.ConversationData.ContainsKey(channelData.Channel.Id))
                        {
                            members = context.ConversationData.GetValue<List<string>>(channelData.Channel.Id).ToArray();
                            context.ConversationData.RemoveValue(channelData.Channel.Id);
                        }
                        else
                        {
                            IList<ChannelAccount> allMembers = await GetAllMembers(connector, channelData);
                            members = allMembers.Select(m => m.Id).ToArray();
                        }
                    }
                    foreach (var memberId in members)
                    {
                        // Create or get existing chat conversation with user
                        try
                        {
                            await SendNotification(context, reply, memberId, actionInfo.Value);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
            }
        }

        private static async Task SendNotification(IDialogContext context, Activity reply, string memberId, string notificationType)
        {
            var userId = memberId.Trim();
            var botId = context.Activity.Recipient.Id;
            var botName = context.Activity.Recipient.Name;

            var channelData = context.Activity.GetChannelData<TeamsChannelData>();
            var connectorClient = new ConnectorClient(new Uri(context.Activity.ServiceUrl));

            var parameters = new ConversationParameters
            {
                Bot = new ChannelAccount(botId, botName),
                Members = new ChannelAccount[] { new ChannelAccount(userId) },
                ChannelData = new TeamsChannelData
                {
                    Tenant = channelData.Tenant,
                }
            };

            var conversationResource = await connectorClient.Conversations.CreateConversationAsync(parameters);
            var replyMessage = Activity.CreateMessageActivity();
            replyMessage.From = new ChannelAccount(botId, botName);
            replyMessage.Conversation = new ConversationAccount(id: conversationResource.Id.ToString());
            replyMessage.ChannelData = new TeamsChannelData() { Notification = new NotificationInfo(true) };

            switch (notificationType)
            {
                case "Weather":
                    reply.Summary = "Here are weather updates";
                    break;
                case "OperationsDelay":
                    reply.Summary = "Operation delay due to bad weather";
                    break;
                case "SocialEvents":
                    reply.Summary = "Here are few social events";
                    break;
                default:
                    break;
            }

            replyMessage.Attachments = reply.Attachments;

            replyMessage.AttachmentLayout = reply.AttachmentLayout;

            try
            {
                await connectorClient.Conversations.SendToConversationAsync(conversationResource.Id, (Activity)replyMessage);
            }
            catch (Exception ex)
            {
                // Log the exception.
                Console.WriteLine(ex);
            }
        }

        private List<Attachment> GetSocialEvents()
        {
            var url = "https://notificationbotdemo.azurewebsites.net/";
            // var url = "https://fb66644e.ngrok.io";
            var card1 = new HeroCard(text: $"<b>Tenderfoot Adventures</b><br><br>We are a group of retired folks, a good mix of couples and singles, over 55 and keeping physically, socially and mentally active as we enjoy our retirement years. We have a commitment to stay fit for life, to live life with joy and energy, taking responsibility toward our health.")
            {
                Buttons = new List<CardAction>()
                    {
                        new CardAction() { Title = "View Details", Type = ActionTypes.OpenUrl, Value = "https://www.meetup.com/tenderfootadventures/" }
                    },
                Images = new List<CardImage>()
                {
                    new CardImage ( ) { Url  = url + "/public/Resources/tenderfootadventures.png"}
                }

            }.ToAttachment();

            var card2 = new HeroCard(text: $"<b>Seattle Social Tennis Club</b><br><br>The intent of this group is to promote the great sport of tennis and set up a convenient way for people to network with other people in the Seattle area who share the same passion and interest. Membership is completely free. I generally play in Green Lake, East Lake, Capital Hill (Seattle University), Magnolia neighborhoods.")
            {
                Buttons = new List<CardAction>()
                    {
                        new CardAction() { Title = "View Details", Type = ActionTypes.OpenUrl, Value = "https://www.meetup.com/Seattle-Social-Tennis-Club/" }
                    },
                Images = new List<CardImage>()
                {
                    new CardImage ( ) { Url  = url + "/public/Resources/seattle-social-tennis-club.png"}
                }

            }.ToAttachment();

            var card3 = new HeroCard(text: $"<b>Silverdale Social Ladies</b><br><br>This is a group for all women who want to meet others, socialize, get up, go out and have fun! <br> Come join us for: Coffee dates, weekday afternoon walks, weekend hikes, dinner dates, movie nights, paint & sip events, concerts, yoga in the park, etc!")
            {
                Buttons = new List<CardAction>()
                    {
                      new CardAction() { Title = "View Details", Type = ActionTypes.OpenUrl, Value = "https://www.meetup.com/Silverdale-Active-Ladies/" }
                    },
                Images = new List<CardImage>()
                {
                    new CardImage ( ) { Url  = url + "/public/Resources/silverdale-active-ladies.png"}
                }

            }.ToAttachment();
            var list = new List<Attachment>();
            list.Add(card1);
            list.Add(card2);
            list.Add(card3);
            return list;
        }

        private Attachment GetOperationsDelayCard()
        {
            try
            {
                // Get a JSON-serialized payload
                // Your app will probably get cards from somewhere else :)
                var jsonPath = System.Web.Hosting.HostingEnvironment.MapPath("~/Resources/OperationsDelay.json");
                var json = System.IO.File.ReadAllText(jsonPath);
                // Parse the JSON 
                AdaptiveCardParseResult result = AdaptiveCard.FromJson(json);

                // Get card from result
                AdaptiveCard card = result.Card;
                Attachment attachment = new Attachment();
                attachment.ContentType = AdaptiveCard.ContentType;
                attachment.Content = card;
                return attachment;
            }
            catch (AdaptiveSerializationException ex)
            {
                // Failed to deserialize card 
                // This occurs from malformed JSON
                // or schema violations like required properties missing 
            }
            return null;
        }

        private Attachment GetWeatherCard()
        {
            try
            {
                // Get a JSON-serialized payload
                // Your app will probably get cards from somewhere else :)
                var jsonPath = System.Web.Hosting.HostingEnvironment.MapPath("~/Resources/Weather.json");

                var json = System.IO.File.ReadAllText(jsonPath);
                json = json.Replace("##Date##", DateTime.Now.ToString("dddd, dd MMMM yyyy"));
                // Parse the JSON 
                AdaptiveCardParseResult result = AdaptiveCard.FromJson(json);

                // Get card from result
                AdaptiveCard card = result.Card;
                Attachment attachment = new Attachment();
                attachment.ContentType = AdaptiveCard.ContentType;
                attachment.Content = card;
                return attachment;
            }
            catch (AdaptiveSerializationException ex)
            {
                // Failed to deserialize card 
                // This occurs from malformed JSON
                // or schema violations like required properties missing 
            }
            return null;
        }

        public static Attachment GetWelcomeMessage(string actionId, IList<ChannelAccount> members)
        {
            var section = new O365ConnectorCardSection("Please select the type of notification you want to receive", null, null, null, null);
            var notificationCardAction = new O365ConnectorCardActionCard(
                O365ConnectorCardActionCard.Type,
                "Select Notification",
                "notificationType",
                 new List<O365ConnectorCardInputBase>
                {
                    new  O365ConnectorCardMultichoiceInput(
                        O365ConnectorCardMultichoiceInput.Type,
                        "notificationType",
                        true,
                        "Select Notification",
                        null,
                        new List<O365ConnectorCardMultichoiceInputChoice>
                        {
                            new O365ConnectorCardMultichoiceInputChoice("Weather", "Weather"),
                            new O365ConnectorCardMultichoiceInputChoice("Operations Delay", "OperationsDelay"),
                            new O365ConnectorCardMultichoiceInputChoice("Social Events", "SocialEvents")
                        },
                        "compact"
                        ,false)
                 },
                new List<O365ConnectorCardActionBase>
                {
                   new O365ConnectorCardHttpPOST(
                        O365ConnectorCardHttpPOST.Type,
                        "Show notification",
                        "notificationType",
                        @"{""Value"":""{{notificationType.value}}"",  ""ActionId"":"""+ actionId + @"""  "+ (members==null?"":@",""Members"":""{{members.value}}""") + "}")
                });

            if (members != null)
            {
                var memberSelection = new O365ConnectorCardMultichoiceInput(
                    O365ConnectorCardMultichoiceInput.Type,
                    "members",
                    false,
                    "Select Members",
                    null, new List<O365ConnectorCardMultichoiceInputChoice>()
                    ,
                    "compact"
                    , true);

                memberSelection.Choices.Add(new O365ConnectorCardMultichoiceInputChoice("ALL TEAM", "ALL TEAM"));

                foreach (var member in members)
                {
                    var nameParts = member.Name.Split(' ');
                    var fullName = string.Empty;
                    for (int i = 0; i < nameParts.Length && i < 2; i++)
                    {
                        if (!string.IsNullOrEmpty(fullName))
                            fullName += " ";
                        fullName += nameParts[i].Trim();
                    }

                    memberSelection.Choices.Add(new O365ConnectorCardMultichoiceInputChoice(fullName, member.Id));
                }

                notificationCardAction.Inputs.Add(memberSelection);
            }


            O365ConnectorCard card = new O365ConnectorCard()
            {
                ThemeColor = "#E67A9E",
                Title = "Welcome to Notification Bot",
                Summary = "",
                Sections = new List<O365ConnectorCardSection> { section },
                PotentialAction = new List<O365ConnectorCardActionBase>
                {
                    notificationCardAction
                }
            };
            return card.ToAttachment();
        }
    }
}