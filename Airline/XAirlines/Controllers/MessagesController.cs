using Airlines.XAirlines.Common;
using Airlines.XAirlines.Dialogs;
using Airlines.XAirlines.Helpers;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Airlines.XAirlines.Controllers
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        [HttpPost]
        public async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            switch (activity.Type)
            {
                case ActivityTypes.Message:
                    await Conversation.SendAsync(activity, () => new RootDialog());
                    break;
                case ActivityTypes.Invoke:
                    return await HandleInvokeActivity(activity);
                case ActivityTypes.ConversationUpdate:
                    await HandleConversationUpdate(activity);
                    break;
            }
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }

        /// <summary>
        /// Handle an invoke activity.
        /// </summary>
        private async Task<HttpResponseMessage> HandleInvokeActivity(Activity activity)
        {
            var activityValue = activity.Value.ToString();
            switch (activity.Name)
            {
                case "signin/verifyState":
                    await Conversation.SendAsync(activity, () => new RootDialog());
                    break;
                case "composeExtension/query":
                // Handle fetching task module content
                case "task/fetch":
                // Handle fetching task module content
                case "task/submit":
                    // Handle submission of task module info
                    // Run this on a task so that 
                    break;
            }
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }

        /// <summary>
        /// Handle request to fetch task module content.
        /// </summary>
        private static async Task HandleConversationUpdate(Activity message)
        {
            ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
            var channelData = message.GetChannelData<TeamsChannelData>();
            // Treat 1:1 add/remove events as if they were add/remove of a team member
            if (channelData.EventType == null)
            {
                if (message.MembersAdded != null)
                    channelData.EventType = "teamMemberAdded";
                if (message.MembersRemoved != null)
                    channelData.EventType = "teamMemberRemoved";
            }
            switch (channelData.EventType)
            {
                case "teamMemberAdded":
                    // Team member was added (user or bot)
                    if (message.MembersAdded.Any(m => m.Id.Contains(message.Recipient.Id)))
                    {
                        // Bot was added to a team: send welcome message
                        message.Text = "hi";
                        await Conversation.SendAsync(message, () => new RootDialog());
                    }
                    break;
                case "teamMemberRemoved":
                    // Add team & channel details 
                    if (message.MembersRemoved.Any(m => m.Id.Contains(message.Recipient.Id)))
                    {
                        // Bot was removed from a team: remove entry for the team in the database
                    }
                    else
                    {
                        // Member was removed from a team: update the team member  count
                    }
                    break;
                // Update the team and channel info in the database when the team is rename or when channel are added/removed/renamed
                case "teamRenamed":
                    // Rename team & channel details 
                    break;
                case "channelCreated":
                    break;
                case "channelRenamed":
                    break;
                case "channelDeleted":
                    break;
                default:
                    break;
            }
        }
    }
}