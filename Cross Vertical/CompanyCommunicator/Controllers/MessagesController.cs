// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// MIT License
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
using CrossVertical.Announcement.Dialogs;
using CrossVertical.Announcement.Helper;
using CrossVertical.Announcement.Helpers;
using CrossVertical.Announcement.Models;

using CrossVertical.Announcement.Repository;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TaskModule;

namespace CrossVertical.Announcement.Controllers
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

                case ActivityTypes.MessageReaction:
                    await HandleReactions(activity);
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
                    using (var connector = new ConnectorClient(new Uri(activity.ServiceUrl)))
                    {
                        var channelData = activity.GetChannelData<TeamsChannelData>();
                        var tid = channelData.Tenant.Id;

                        string currentUser = await RootDialog.GetCurrentUserEmailId(activity);
                        if (currentUser == null)
                        {
                            return new HttpResponseMessage(HttpStatusCode.OK);
                        }
                        var response = await MessageExtension.HandleMessageExtensionQuery(connector, activity, tid, currentUser);
                        return response != null
                            ? Request.CreateResponse(response)
                            : new HttpResponseMessage(HttpStatusCode.OK);
                    }

                case "task/fetch":
                    // Handle fetching task module content
                    return await HandleTaskModuleFetchRequest(activity, activityValue);

                case "task/submit":
                    // Handle submission of task module info
                    // Run this on a task so that 
                    new Task(async () =>
                    {
                        var action = JsonConvert.DeserializeObject<TaskModule.BotFrameworkCardValue<ActionDetails>>(activityValue);
                        activity.Name = action.Data.ActionType;
                        await Conversation.SendAsync(activity, () => new RootDialog());
                    }).Start();

                    await Task.Delay(TimeSpan.FromSeconds(2));// Give it some time to start showing output.
                    break;
            }
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }

        /// <summary>
        /// Handle request to fetch task module content.
        /// </summary>
        private async Task<HttpResponseMessage> HandleTaskModuleFetchRequest(Activity activity, string activityValue)
        {
            var action = JsonConvert.DeserializeObject<TaskModule.TaskModuleActionData<ActionDetails>>(activityValue);

            var channelData = activity.GetChannelData<TeamsChannelData>();
            var tenantId = channelData.Tenant.Id;

            // Default to common parameters for task module
            var taskInfo = new TaskModuleTaskInfo()
            {
                Title = ApplicationSettings.AppName,
                Height = 900,
                Width = 600,
            };

            // Populate the task module content, based on the kind of dialog requested
            Attachment card = null;
            switch (action.Data.Data.ActionType)
            {
                case Constants.CreateOrEditAnnouncement:
                    taskInfo.Title = "Create New";
                    card = await CardHelper.GetCreateNewAnnouncementCard(tenantId);
                    break;
                case Constants.ShowMoreDetails:
                    taskInfo.Title = "Details";
                    var showDetails = JsonConvert.DeserializeObject<TaskModule.TaskModuleActionData<AnnouncementActionDetails>>(activityValue);
                    card = await CardHelper.GetPreviewAnnouncementCard(showDetails.Data.Data.Id);
                    taskInfo.Height = 900;
                    taskInfo.Width = 600;

                    break;
                case Constants.ShowEditAnnouncementTaskModule:
                    taskInfo.Title = "Edit a message";
                    var editAnnouncement = JsonConvert.DeserializeObject<TaskModule.TaskModuleActionData<AnnouncementActionDetails>>(activityValue);

                    var campaign = await Cache.Announcements.GetItemAsync(editAnnouncement.Data.Data.Id);
                    if (campaign == null || campaign.Status == Status.Sent)
                    {
                        card = CardHelper.GetUpdateMessageCard($"This {Helper.ApplicationSettings.AppFeature} is already sent and not allowed to edit.");
                        taskInfo.Height = 100;
                        taskInfo.Width = 500;
                    }
                    else
                        card = await CardHelper.GetEditAnnouncementCard(editAnnouncement.Data.Data.Id, tenantId);
                    break;
                default:
                    break;
            }

            taskInfo.Card = card;
            TaskModuleResponseEnvelope taskModuleEnvelope = new TaskModuleResponseEnvelope
            {
                Task = new TaskModuleContinueResponse
                {
                    Type = "continue",
                    Value = taskInfo
                }
            };

            return Request.CreateResponse(HttpStatusCode.OK, taskModuleEnvelope);
        }

        private static async Task HandleConversationUpdate(Activity message)
        {
            ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
            var channelData = message.GetChannelData<TeamsChannelData>();

            // Ensure that we have an entry for this tenant in the database
            var tenant = await Common.CheckAndAddTenantDetails(channelData.Tenant.Id);

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
                        if (channelData.Team == null)
                        {
                            if (message.From.Id == message.Recipient.Id)
                                return;
                            var userEmailId = await RootDialog.GetCurrentUserEmailId(message);
                            var userFromDB = await Cache.Users.GetItemAsync(userEmailId);
                            if (userFromDB != null)
                                return;
                        }
                        // Bot was added to a team: send welcome message
                        message.Text = Constants.ShowWelcomeScreen;
                        await Conversation.SendAsync(message, () => new RootDialog());

                        await AddTeamDetails(message, channelData, tenant);
                    }
                    else
                    {
                        // Member was added to a team: update the team member count
                        await UpdateTeamCount(message, channelData, tenant);
                    }
                    break;
                case "teamMemberRemoved":
                    // Add team & channel details 
                    if (message.MembersRemoved.Any(m => m.Id.Contains(message.Recipient.Id)))
                    {
                        // Bot was removed from a team: remove entry for the team in the database
                        await RemoveTeamDetails(channelData, tenant);
                    }
                    else
                    {
                        // Member was removed from a team: update the team member  count
                        await UpdateTeamCount(message, channelData, tenant);
                    }
                    break;
                // Update the team and channel info in the database when the team is rename or when channel are added/removed/renamed
                case "teamRenamed":
                    // Rename team & channel details 
                    await RenameTeam(channelData, tenant);
                    break;

                case "channelCreated":
                    await AddNewChannelDetails(channelData, tenant);
                    break;
                case "channelRenamed":
                    await RenameChannel(channelData, tenant);
                    break;

                case "channelDeleted":
                    await DeleteChannel(channelData, tenant);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Handle messageReaction events, which indicate user liked/unliked a message sent by the bot.
        /// </summary>
        private static async Task HandleReactions(Activity message)
        {
            if (message.ReactionsAdded != null || message.ReactionsRemoved != null)
            {
                // Determine if likes were net added or removed
                var reactionToAdd = message.ReactionsAdded != null ? 1 : -1;
                var channelData = message.GetChannelData<TeamsChannelData>();
                var replyToId = message.ReplyToId;
                if (channelData.Team != null)
                    replyToId = message.Conversation.Id;

                // Look for the announcement that was liked/unliked, and update the reaction count on that announcement
                var tenant = await Cache.Tenants.GetItemAsync(channelData.Tenant.Id);
                bool messageFound = false;
                foreach (var announcementId in tenant.Announcements)
                {
                    var announcement = await Cache.Announcements.GetItemAsync(announcementId);
                    if (announcement?.Recipients == null)
                        continue;

                    if (channelData.Team == null)
                        foreach (var group in announcement.Recipients.Groups)
                        {
                            var user = group.Users.FirstOrDefault(u => u.MessageId == replyToId);
                            if (user != null)
                            {
                                messageFound = true;
                                user.LikeCount = reactionToAdd == 1 ? 1 : 0;
                            }
                        }
                    if (!messageFound && channelData.Team != null)
                        foreach (var channel in announcement.Recipients.Channels)
                        {
                            if (channel.Channel.MessageId == replyToId)
                            {
                                var EmailId = await RootDialog.GetCurrentUserEmailId(message);
                                if (message.ReactionsAdded != null && message.ReactionsAdded.Count != 0)
                                {
                                    if (!channel.LikedUsers.Contains(EmailId))
                                        channel.LikedUsers.Add(EmailId);
                                }
                                else if (message.ReactionsRemoved != null)
                                {
                                    if (channel.LikedUsers.Contains(EmailId))
                                        channel.LikedUsers.Remove(EmailId);
                                }
                                messageFound = true;
                                break;

                            }
                        }
                    if (messageFound)
                    {
                        await Cache.Announcements.AddOrUpdateItemAsync(announcement.Id, announcement);
                        break;
                    }
                }
            }
        }

        private static async Task DeleteChannel(TeamsChannelData channelData, Tenant tenant)
        {
            var team = await GetTeam(channelData, tenant);
            if (team != null)
            {
                var channel = team.Channels.FirstOrDefault(c => c.Id == channelData.Channel.Id);
                if (channel != null)
                {
                    team.Channels.Remove(channel);
                    await Cache.Teams.AddOrUpdateItemAsync(team.Id, team);
                }
            }
        }

        private static async Task RenameChannel(TeamsChannelData channelData, Tenant tenant)
        {
            var team = await GetTeam(channelData, tenant);
            if (team != null)
            {
                var channel = team.Channels.FirstOrDefault(c => c.Id == channelData.Channel.Id);
                if (channel != null)
                {
                    channel.Name = channelData.Channel.Name;
                    await Cache.Teams.AddOrUpdateItemAsync(team.Id, team);
                }
            }
        }

        private static async Task AddNewChannelDetails(TeamsChannelData channelData, Tenant tenant)
        {
            var team = await GetTeam(channelData, tenant);
            if (team != null)
            {
                team.Channels.Add(new Channel() { Id = channelData.Channel.Id, Name = channelData.Channel.Name });
                await Cache.Teams.AddOrUpdateItemAsync(team.Id, team);
            }

        }

        private static async Task<Team> GetTeam(TeamsChannelData channelData, Tenant tenant)
        {
            var teamId = channelData.Team.Id;
            if (tenant.Teams.Contains(channelData.Team.Id))
            {
                return await Cache.Teams.GetItemAsync(channelData.Team.Id);
            }
            return null;
        }

        private static async Task RenameTeam(TeamsChannelData channelData, Tenant tenant)
        {
            var team = await GetTeam(channelData, tenant);
            if (team != null)
            {
                team.Name = channelData.Team.Name;
                await Cache.Teams.AddOrUpdateItemAsync(team.Id, team);
            }
        }

        private static async Task RemoveTeamDetails(TeamsChannelData channelData, Tenant tenant)
        {
            var team = await GetTeam(channelData, tenant);
            if (team != null)
            {
                await Cache.Teams.DeleteItemAsync(team.Id);
            }
            tenant.Teams.Remove(channelData.Team.Id);
            await Cache.Tenants.AddOrUpdateItemAsync(tenant.Id, tenant);

        }

        private static async Task AddTeamDetails(Activity message, TeamsChannelData channelData, Tenant tenant)
        {
            if (channelData.Team != null)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
                var members = await connector.Conversations.GetConversationMembersAsync(channelData.Team.Id);
                int count = members.Count;
                Team team = null;
                if (tenant.Teams.Contains(channelData.Team.Id))
                {
                    team = await Cache.Teams.GetItemAsync(channelData.Team.Id);
                }
                else
                {
                    var teamChannelData = message.GetChannelData<TeamsChannelDataExt>();
                    team = new Team
                    {
                        Id = channelData.Team.Id,
                        AadObjectId = teamChannelData.Team.AADGroupId
                    };
                }

                // Update the members.
                team.Name = channelData.Team.Name;
                team.Members = members.Select(m => m.AsTeamsChannelAccount().UserPrincipalName.ToLower()).ToList();

                // Add all teams and channels
                ConversationList channels = connector.GetTeamsConnectorClient().Teams.FetchChannelList(message.GetChannelData<TeamsChannelData>().Team.Id);
                foreach (var channel in channels.Conversations)
                {
                    team.Channels.Add(new Channel() { Id = channel.Id, Name = channel.Name ?? "General" });
                }
                await Cache.Teams.AddOrUpdateItemAsync(team.Id, team);

                tenant.Teams.Add(channelData.Team.Id);

                await Cache.Tenants.AddOrUpdateItemAsync(tenant.Id, tenant);

                await SendWelcomeMessageToAllMembers(tenant, message, channelData, members.AsTeamsChannelAccounts());

            }
        }

        private static async Task SendWelcomeMessageToAllMembers(Tenant tenant, Activity message, TeamsChannelData channelData, IEnumerable<TeamsChannelAccount> members)
        {
            var tid = channelData.Tenant.Id;
            var currentUser = members.FirstOrDefault(m => m.Id == message.From.Id)?.AsTeamsChannelAccount()?.UserPrincipalName?.ToLower();
            var tenatInfo = await Cache.Tenants.GetItemAsync(tid);
            var moderatorCard = CardHelper.GetWelcomeScreen(false, Role.Moderator);
            var userCard = CardHelper.GetWelcomeScreen(false, Role.User);
            var newUsers = new List<User>();

            foreach (var member in members)
            {
                var emailId = member.UserPrincipalName.ToLower().Trim();

                emailId = Common.RemoveHashFromGuestUserUPN(emailId);

                if (!tenatInfo.Users.Contains(emailId))
                {
                    var userDetails = new User()
                    {
                        BotConversationId = member.Id,
                        Id = emailId,
                        Name = member.Name ?? member.GivenName,
                        AadObjectId = member.AadObjectId
                        
                    };
                    tenant.Users.Add(userDetails.Id);
                    newUsers.Add(userDetails);
                }
            }

            var importResult = await DocumentDBRepository.BulkExecutor.BulkImportAsync(newUsers);
            if (importResult.NumberOfDocumentsImported != newUsers.Count)
            {
                // TODO: Take action
            }

            // Save all the user's details in the tenant.
            await Cache.Tenants.AddOrUpdateItemAsync(tenant.Id, tenant);

            // Comment this if you don't want to send welcome message to each new user.
            if (tenant.IsAdminConsented && newUsers.Count > 0)
            {

                var serviceUrl = message.ServiceUrl;
                var tenantId = channelData.Tenant.Id;
                var results = new ConcurrentBag<NotificationSendStatus>();
                await Common.ForEachAsync(newUsers, ApplicationSettings.NoOfParallelTasks,
                async userDetails =>
                {
                    Attachment card = tenant.Moderators.Contains(userDetails.Id) ? moderatorCard : userCard;
                    var result = await ProactiveMessageHelper.SendPersonalNotification(serviceUrl, tenantId, userDetails, null, card);
                    results.Add(result);
                });

                var owner = await Cache.Users.GetItemAsync(currentUser);
                if (owner != null)
                {
                    var successCount = results.Count(m => m.IsSuccessful);

                    await ProactiveMessageHelper.SendPersonalNotification(serviceUrl, tenantId, owner,
                                                $"Process of sending welcome message to all members of {channelData.Team.Name} completed. Successful: {successCount}. " +
                                                $"Failure: {results.Count - successCount}.", null);
                }
            }
        }

        private static async Task UpdateTeamCount(Activity message, TeamsChannelData channelData, Tenant tenant)
        {
            if (tenant.Teams.Contains(channelData.Team.Id))
            {
                ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
                var members = await connector.Conversations.GetConversationMembersAsync(channelData.Team.Id);

                var team = await Cache.Teams.GetItemAsync(channelData.Team.Id);
                team.Members = members.Select(m => m.AsTeamsChannelAccount().UserPrincipalName.ToLower()).ToList();
                await Cache.Teams.AddOrUpdateItemAsync(team.Id, team);
            }

        }
    }
}
