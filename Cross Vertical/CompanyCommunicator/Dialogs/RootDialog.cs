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
using CrossVertical.Announcement.Helper;
using CrossVertical.Announcement.Helpers;
using CrossVertical.Announcement.Models;
using CrossVertical.Announcement.Repository;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CrossVertical.Announcement.Dialogs
{

    /// <summary>
    /// This Dialog enables the user to create, view and edit announcements.
    /// </summary>
    [Serializable]
    public class RootDialog : IDialog<object>
    {

        /// <summary>
        /// Called when the dialog is started.
        /// </summary>
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        /// <summary>
        /// Called when a message is received by the dialog
        /// </summary>
        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            string message = string.Empty;
            if (activity.Text != null)
                message = Microsoft.Bot.Connector.Teams.ActivityExtensions.GetTextWithoutMentions(activity).ToLowerInvariant();
            // Check if User or Team is registered in the database, and store the information in ConversationData for quick retrieval later.
            string profileKey = GetKey(activity, Constants.ProfileKey);
            User userDetails = null;
            var channelData = context.Activity.GetChannelData<TeamsChannelData>();

            if (context.ConversationData.ContainsKey(profileKey))
            {
                userDetails = context.ConversationData.GetValue<User>(profileKey);
            }
            else
            {
                userDetails = await CheckAndAddUserDetails(activity, channelData);
                if (userDetails == null)
                    return;
                context.ConversationData.SetValue(profileKey, userDetails);
            }


            Tenant tenantData = await Common.CheckAndAddTenantDetails(channelData.Tenant.Id);
            Role role = Common.GetUserRole(userDetails.Id, tenantData);

            if (!tenantData.IsAdminConsented)
            {
                if (channelData.Team == null)
                    await SendOAuthCardAsync(context, activity);
                else
                {
                    var reply = activity.CreateReply();
                    reply.Attachments.Add(CardHelper.GetCardForNonConsentedTenant());
                    await context.PostAsync(reply);
                }
                return;
            }
            if (activity.Attachments != null && activity.Attachments.Any(a => a.ContentType == FileDownloadInfo.ContentType))
            {
                // Excel file with tenant-level group information was uploaded: update the tenant info in the database
                var attachment = activity.Attachments.First();
                await HandleExcelAttachement(context, attachment, channelData);
            }
            else if (activity.Value != null)
            {
                // Handle clicks on adaptive card buttons, whether from a card in the conversation, or in a task module
                await HandleActions(context, activity, tenantData, userDetails);
            }
            else
            {
                if (message.ToLowerInvariant().Contains("refresh photos") && role == Role.Admin)
                {
                    await RefreshProfilePhotos(context, activity, tenantData, userDetails);
                }
                else if (message.ToLowerInvariant().Contains("clear cache"))
                {
                    await ClearCache(context);
                }
                else
                {
                    var reply = activity.CreateReply();
                    if (channelData.Team != null)
                    {
                        if (message != Constants.ShowWelcomeScreen.ToLower())
                        {
                            reply.Text = "Announcements app is notification only in teams and channels. Please use the app in 1:1 chat to interact meaningfully.";
                            await context.PostAsync(reply);
                            return;
                        }
                    }

                    if (tenantData.Admin == userDetails.Id || tenantData.Moderators.Contains(userDetails.Id))
                        reply.Attachments.Add(CardHelper.GetWelcomeScreen(channelData.Team != null, role));
                    else
                    {
                        reply.Attachments.Add(CardHelper.GetWelcomeScreen(channelData.Team != null, role));
                    }

                    await context.PostAsync(reply);
                }
            }
        }

        #region Sign In Flow

        private async Task SendOAuthCardAsync(IDialogContext context, Activity activity)
        {
            var reply = await context.Activity.CreateOAuthReplyAsync(ApplicationSettings.ConnectionName,
                "To start using this app, you'll first need to sign in.", "Sign In", true).ConfigureAwait(false);
            await context.PostAsync(reply);

            context.Wait(WaitForToken);
        }

        private async Task WaitForToken(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            var tokenResponse = activity.ReadTokenResponseContent();
            var channelData = context.Activity.GetChannelData<TeamsChannelData>();
            if (tokenResponse != null)
            {
                // Use the token to do exciting things!
                await SendGrantAdminConsentCard(context, activity, channelData);
            }
            else
            {
                // Get the Activity Message as well as activity.value in case of Auto closing of pop-up
                string input = activity.Type == ActivityTypes.Message ? Microsoft.Bot.Connector.Teams.ActivityExtensions.GetTextWithoutMentions(activity)
                                                                : ((dynamic)(activity.Value)).state.ToString();
                if (!string.IsNullOrEmpty(input))
                {
                    tokenResponse = await context.GetUserTokenAsync(ApplicationSettings.ConnectionName, input.Trim());
                    if (tokenResponse != null)
                    {
                        try
                        {
                            await context.PostAsync($"Your sign in was successful. Please grant the application permissions.");
                            await SendGrantAdminConsentCard(context, activity, channelData);
                        }
                        catch (Exception ex)
                        {
                            ErrorLogService.LogError(ex);
                        }

                        context.Wait(MessageReceivedAsync);
                        return;
                    }
                }
                await context.PostAsync($"Hmm. Something went wrong. Please initiate the SignIn again. Try sending help.");
                context.Wait(MessageReceivedAsync);
            }
        }

        #endregion

        #region Handle Actions

        private async Task HandleActions(IDialogContext context, Activity activity, Tenant tenant, User userDetails)
        {
            var channelData = context.Activity.GetChannelData<TeamsChannelData>();

            // Get the kind of action that was requested
            // The structure is slightly different if the button was on a task module, or on a card sent by the bot 
            string type = string.Empty;
            try
            {
                // Try to parse it as a task module button first
                var details = JsonConvert.DeserializeObject<TaskModule.BotFrameworkCardValue<ActionDetails>>(activity.Value.ToString());
                type = details.Data.ActionType;
            }
            catch (Exception)
            {
                var details = JsonConvert.DeserializeObject<ActionDetails>(activity.Value.ToString());
                type = details.ActionType;
            }

            var role = Common.GetUserRole(userDetails.Id, tenant);

            // For user role, only these actions are allowed.
            if (role == Role.User &&
                type != Constants.Acknowledge &&
                type != Constants.ShowRecents &&
                type != Constants.ShowSentAnnouncement)
            {
                await context.PostAsync("You do not have permissions to perform this task.");
                return;
            }

            switch (type)
            {
                case Constants.CreateOrEditAnnouncement:
                case Constants.EditAnnouncementFromTab:
                    // Save in DB & Send preview card
                    await CreateOrEditAnnouncement(context, activity, channelData);
                    break;
                case Constants.ConfigureAdminSettings:
                    await SendAdminPanelCard(context, activity, channelData);
                    break;
                case Constants.CreateGroupWithAllEmployees:
                    // Allow user to configure the groups.
                    await CreateGroupWithAllEmployees(context, activity, channelData);
                    break;
                case Constants.CreateTeamsWithAllEmployees:
                    // Allow user to configure the groups.
                    await CreateAllEmployeeTeam(context, activity, channelData);
                    break;
                case Constants.ConfigureGroups:
                    // Allow user to configure the groups.
                    await SendUpdateGroupConfigurationCard(context, activity, channelData);
                    break;
                case Constants.SetModerators:
                    // Allow user to configure the groups.
                    await SetModerators(context, activity, channelData);
                    break;
                case Constants.SendAnnouncement:
                case Constants.ScheduleAnnouncement:
                    try
                    {
                        new Task(async () =>
                                    {
                                        await SendOrScheduleAnnouncement(type, context, activity, channelData);
                                    }).Start();
                    }
                    catch (Exception ex)
                    {
                        ErrorLogService.LogError(ex);
                    }
                    break;
                case Constants.Acknowledge:
                    await SaveAcknowledgement(context, activity, channelData);
                    break;
                case Constants.ShowAllDrafts:
                    await ShowAllDrafts(context, activity, channelData);
                    break;
                case Constants.ShowAnnouncement:
                    await ShowAnnouncementDraft(context, activity, channelData);
                    break;
                case Constants.ShowRecents:
                    await ShowRecentAnnouncements(context, activity, channelData);
                    break;
                case Constants.ShowSentAnnouncement:
                    await SendPreviewOfSentAnnouncement(context, activity);
                    break;
                default:
                    break;
            }
        }

        private async Task CreateAllEmployeeTeam(IDialogContext context, Activity activity, TeamsChannelData channelData)
        {
            try
            {
                var startTime = DateTime.Now;
                Tenant tenantData = await Common.CheckAndAddTenantDetails(channelData.Tenant.Id);

                await context.PostAsync("Fetching all users present in this tenant. This may take time depending on number of employees.");

                // Fetch access token.
                var token = await GraphHelper.GetAccessToken(channelData.Tenant.Id, ApplicationSettings.AppId, ApplicationSettings.AppSecret);
                var graphHelper = new GraphHelper(token);

                // Fetch all team members in tenant
                var allMembers = await graphHelper.FetchAllTenantMembersAsync();

                var maxTeamSizeSupported = 4999;

                await context.PostAsync($"Fetched {allMembers.Count} members. Now creating { Math.Ceiling(((decimal)allMembers.Count / maxTeamSizeSupported))} team/s with all the member. This may take time, please wait.");

                var startIndex = 0;

                int TeamCount = 0;
                while (startIndex < allMembers.Count)
                {
                    List<string> userIds = allMembers.Skip(startIndex).Take(maxTeamSizeSupported).Select(c => c.id).ToList();
                    var teamName = Constants.AllEmployeesGroupAndTeamName + (TeamCount == 0 ? "" : " " + TeamCount);
                    var teamId = await graphHelper.CreateNewTeam(new NewTeamDetails()
                    {
                        TeamName = teamName,
                        OwnerADIds = new List<string> { activity.From.AadObjectId },
                        UserADIds = userIds
                    });

                    if (teamId == null)
                    {
                        await context.PostAsync($"Unable to create new team. Please try again later.");
                    }
                    else
                    {
                        await context.PostAsync($"Team \"{teamName}\" created with {userIds.Count} members. Please wait till all the members are synced and then install {ApplicationSettings.AppName} app.");
                    }

                    startIndex += maxTeamSizeSupported;
                    TeamCount++;
                }
                var endTime = DateTime.Now;
                var difference = endTime - startTime;

                await context.PostAsync($"All teams created successfully. \n\n Teams created: {TeamCount} \n\n Users Added: {allMembers.Count} \n\n  Time taken: { difference.Minutes} mins");

            }
            catch (Exception ex)
            {
                ErrorLogService.LogError(ex);
                await context.PostAsync($"Process failed. Please try again.");
            }
            // Create a new Team with all members
        }

        private async Task CreateGroupWithAllEmployees(IDialogContext context, Activity activity, TeamsChannelData channelData)
        {
            try
            {
                var startTime = DateTime.Now;
                Tenant tenantData = await Common.CheckAndAddTenantDetails(channelData.Tenant.Id);

                await context.PostAsync("Fetching all users present in this tenant. This may take time depending on number of employees.");

                var token = await GraphHelper.GetAccessToken(channelData.Tenant.Id, ApplicationSettings.AppId, ApplicationSettings.AppSecret);
                var graphHelper = new GraphHelper(token);

                // Fetch all team members in tenant
                var allMembers = await graphHelper.FetchAllTenantMembersAsync();

                await context.PostAsync($"Fetched {allMembers.Count} members.");

                // Create new Group with all the members
                Group groupDetails = new Group
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = Constants.AllEmployeesGroupAndTeamName,
                    Users = allMembers.Select(u => Common.RemoveHashFromGuestUserUPN(u.userPrincipalName.ToLower())).ToList()
                };

                // Update existing group if exists.
                await Common.CreateOrUpdateExistingGroups(new List<Group>() { groupDetails }, tenantData, false);

                await Cache.Tenants.AddOrUpdateItemAsync(tenantData.Id, tenantData);

                await context.PostAsync($"Successfully created new group with all employees ({allMembers.Count}).");
            }
            catch (Exception ex)
            {
                ErrorLogService.LogError(ex);
                await context.PostAsync($"Process failed. Please try again.");
            }
            // Create a new Team with all members
        }

        private async Task ShowRecentAnnouncements(IDialogContext context, Activity activity, TeamsChannelData channelData)
        {
            var tid = channelData.Tenant.Id;
            var emailId = await GetCurrentUserEmailId(activity);

            // Fetch my announcements
            var myTenantAnnouncements = await Common.GetMyAnnouncements(emailId, tid);

            var listCard = new ListCard
            {
                content = new Content()
            };
            listCard.content.title = "Here are all your recent announcements:"; ;
            var list = new List<Item>();
            foreach (var announcement in myTenantAnnouncements.OrderByDescending(a => a.CreatedTime).Take(10))
            {
                if (announcement != null && (announcement.Status == Status.Sent))
                {
                    var item = new Item
                    {
                        icon = announcement.Author.ProfilePhoto,
                        type = "resultItem",
                        id = announcement.Id,
                        title = announcement.Title,
                        subtitle = "Author: " + announcement.Author?.Name
                             + $" | Received Date: { (announcement.Status == Status.Sent ? announcement.CreatedTime.ToShortDateString() : announcement.Schedule.ScheduledTime.Date.ToShortDateString()) }",
                        tap = new Tap()
                        {
                            type = ActionTypes.MessageBack,
                            title = "Id",
                            value = JsonConvert.SerializeObject(new AnnouncementActionDetails()
                            { Id = announcement.Id, ActionType = Constants.ShowSentAnnouncement }) //  "Show Announcement " + announcement.Title + " (" + announcement.Id + ")"
                        }
                    };
                    list.Add(item);
                }
            }

            if (list.Count > 0)
            {
                listCard.content.items = list.ToArray();
                Attachment attachment = new Attachment
                {
                    ContentType = listCard.contentType,
                    Content = listCard.content
                };
                var reply = activity.CreateReply();
                reply.Attachments.Add(attachment);
                await context.PostAsync(reply);
            }
            else
                await context.PostAsync("You don't seem to have any messages received recently. Hang on!");
        }

        private static async Task SendAdminPanelCard(IDialogContext context, Activity activity, TeamsChannelData channelData)
        {
            var reply = activity.CreateReply();
            Tenant tenantData = await Common.CheckAndAddTenantDetails(channelData.Tenant.Id);
            reply.Attachments.Add(CardHelper.GetAdminPanelCard(string.Join(",", tenantData.Moderators)));
            await context.PostAsync(reply);
        }

        private async Task ShowAnnouncementDraft(IDialogContext context, Activity activity, TeamsChannelData channelData)
        {
            var details = JsonConvert.DeserializeObject<AnnouncementActionDetails>(activity.Value.ToString());
            if (details != null)
            {
                var campaign = await Cache.Announcements.GetItemAsync(details.Id);
                if (campaign == null)
                    return;
                await SendPreviewCard(context, activity, campaign, false);
            }
        }

        private static async Task SendPreviewOfSentAnnouncement(IDialogContext context, Activity activity)
        {
            var details = JsonConvert.DeserializeObject<AnnouncementActionDetails>(activity.Value.ToString());
            if (details != null)
            {
                var campaign = await Cache.Announcements.GetItemAsync(details.Id);
                if (campaign == null)
                {
                    await context.PostAsync("This message no longer exists.");
                    return;
                }
                var reply = activity.CreateReply();
                var card = campaign.GetPreviewCard().ToAttachment();
                var userEmailId = await GetCurrentUserEmailId(activity);
                var group = campaign.Recipients.Groups.FirstOrDefault(g => g.Users.Any(u => u.Id.ToLower() == userEmailId.ToLower()));
                if (group != null)
                {
                    var campaignCard = CardHelper.GetCardWithAcknowledgementDetails(card, campaign.Id, userEmailId, group.GroupId);
                    reply.Attachments.Add(campaignCard);
                }
                else
                {
                    var campaignCard = CardHelper.GetCardWithoutAcknowledgementAction(card);
                    reply.Attachments.Add(campaignCard);
                }
                await context.PostAsync(reply);
            }
        }

        private async Task ShowAllDrafts(IDialogContext context, Activity activity, TeamsChannelData channelData)
        {

            var tenatInfo = await Cache.Tenants.GetItemAsync(channelData.Tenant.Id);
            var myTenantAnnouncements = new List<Campaign>();

            var listCard = new ListCard();
            listCard.content = new Content();
            listCard.content.title = "Here are all draft and scheduled announcements:"; ;
            var list = new List<Item>();
            foreach (var announcementId in tenatInfo.Announcements)
            {
                var announcement = await Cache.Announcements.GetItemAsync(announcementId);
                if (announcement != null && (announcement.Status == Status.Draft || announcement.Status == Status.Scheduled))
                {
                    var item = new Item
                    {
                        icon = announcement.Author.ProfilePhoto,
                        type = "resultItem",
                        id = announcement.Id,
                        title = announcement.Title,
                        subtitle = "Author: " + announcement.Author?.Name
                             + $" | Created Date: {announcement.CreatedTime.ToShortDateString()} | { (announcement.Status == Status.Scheduled ? "Scheduled" : "Draft") }",
                        tap = new Tap()
                        {
                            type = ActionTypes.MessageBack,
                            title = "Id",
                            value = JsonConvert.SerializeObject(new AnnouncementActionDetails()
                            { Id = announcement.Id, ActionType = Constants.ShowAnnouncement }) //  "Show Announcement " + announcement.Title + " (" + announcement.Id + ")"
                        }
                    };
                    list.Add(item);
                }
            }

            if (list.Count > 0)
            {
                listCard.content.items = list.ToArray();
                var attachment = new Attachment
                {
                    ContentType = listCard.contentType,
                    Content = listCard.content
                };
                var reply = activity.CreateReply();
                reply.Attachments.Add(attachment);
                await context.PostAsync(reply);
            }
            else
                await context.PostAsync("Thre are no drafts. Please go ahead and create new announcement.");
        }

        private async Task SaveAcknowledgement(IDialogContext context, Activity activity, TeamsChannelData channelData)
        {
            // Get all the details for announcement.
            var details = JsonConvert.DeserializeObject<AnnouncementAcknowledgeActionDetails>(activity.Value.ToString());
            if (details == null || details.Id == null)
            {
                details = JsonConvert.DeserializeObject<TaskModule.BotFrameworkCardValue<AnnouncementAcknowledgeActionDetails>>
                    (activity.Value.ToString()).Data;
            }
            // Add announcement in DB.
            var campaign = await Cache.Announcements.GetItemAsync(details.Id);
            if (campaign == null)
            {
                await context.PostAsync("This campaing has been removed. Please contact campaign owner.");
                return;
            }
            var group = campaign.Recipients.Groups.FirstOrDefault(g => g.GroupId == details.GroupId);
            if (campaign.Status != Status.Sent)
            {
                // await context.PostAsync("This will send acknowlegement when user clicks on it.");
                return;
            }
            if (group != null)
            {
                var user = group.Users.FirstOrDefault(u => u.Id == details.UserId);
                if (user != null)
                {
                    if (!user.IsAcknoledged)
                    {
                        user.IsAcknoledged = true;
                        await Cache.Announcements.AddOrUpdateItemAsync(campaign.Id, campaign);
                        await context.PostAsync("Your response has been recorded.");
                    }
                    else
                        await context.PostAsync("Your response is already recorded.");
                }
            }
        }

        private async Task SendOrScheduleAnnouncement(string type, IDialogContext context, Activity activity, TeamsChannelData channelData)
        {
            // Get all the details for announcement.
            var details = JsonConvert.DeserializeObject<AnnouncementActionDetails>(activity.Value.ToString());
            // Add announcemnet in DB.
            if (details == null || details.Id == null)
            {
                details = JsonConvert.DeserializeObject<TaskModule.BotFrameworkCardValue<AnnouncementActionDetails>>
                    (activity.Value.ToString()).Data;
            }
            var campaign = await Cache.Announcements.GetItemAsync(details.Id);
            if (campaign == null)
            {
                // Error: the annoucement was already sent
                await context.PostAsync("Unable to find this announcement. Please create new announcement.");
                return;
            }
            if (campaign.Status == Status.Sent)
            {
                // Error: no recipients
                await context.PostAsync("This announcement is already sent and can not be resent. Please create new announcement.");
                return;
            }
            else if (type == Constants.SendAnnouncement)
                await context.PostAsync("Please wait while we send this announcement to all recipients.");

            if (campaign.Recipients.Channels.Count == 0 && campaign.Recipients.Groups.Count == 0)
            {
                await context.PostAsync("No recipients. Please select at least one recipient.");
                return;
            }

            // Handle old records.
            if (string.IsNullOrEmpty(campaign.Recipients.TenantId))
            {
                campaign.Recipients.TenantId = channelData.Tenant.Id;
                campaign.Recipients.ServiceUrl = activity.ServiceUrl;
            }

            // Send or schedule the announcement, depending on what the user clicked
            if (type == Constants.SendAnnouncement)
                await SendAnnouncement(context, activity, channelData, campaign);
            else
                await ScheduleAnnouncement(context, activity, channelData, campaign);

            var oldAnnouncementDetails = context.ConversationData.GetValueOrDefault<PreviewCardMessageDetails>(campaign.Id);
            if (oldAnnouncementDetails != null)
            {
                ConnectorClient connectorClient = new ConnectorClient(new Uri(activity.ServiceUrl));

                // Update card.
                var updateCard = campaign.GetPreviewCard().ToAttachment();

                var updateMessage = context.MakeMessage();

                updateMessage.Attachments.Add(CardHelper.GetCardToUpdatePreviewCard(updateCard,
                    $"Note: This announcement is { (type == Constants.SendAnnouncement ? "sent" : "scheduled") } successfully."));
                await connectorClient.Conversations.UpdateActivityAsync(activity.Conversation.Id, oldAnnouncementDetails.MessageCardId, (Activity)updateMessage);

                // Update action card.
                var message = type == Constants.SendAnnouncement ? "We have send this announcement successfully. Please create new announcement to send again." :
                    $"We have scheduled this announcement to be sent at {campaign.Schedule.ScheduledTime.ToString("MM/dd/yyyy hh:mm tt")}. Note that announcements scheduled for past date will be sent immediately.";

                var updateAnnouncement = CardHelper.GetUpdateMessageCard(message);
                updateMessage = context.MakeMessage();
                updateMessage.Attachments.Add(updateAnnouncement);
                await connectorClient.Conversations.UpdateActivityAsync(activity.Conversation.Id, oldAnnouncementDetails.MessageActionId, (Activity)updateMessage);
                context.ConversationData.RemoveValue(campaign.Id);
            }
            else if (type == Constants.ScheduleAnnouncement)
            {
                var message = $"We have re-scheduled the announcement to be sent at {campaign.Schedule.ScheduledTime.ToString("MM/dd/yyyy hh:mm tt")}.";
                await context.PostAsync(message);
                var reply = activity.CreateReply();
                reply.Attachments.Add(CardHelper.GetAnnouncementBasicDetails(campaign));

                await context.PostAsync(reply);
            }
        }

        private static async Task ScheduleAnnouncement(IDialogContext context, Activity activity, TeamsChannelData channelData, Campaign campaign)
        {
            // Get all the details for announcement.
            var details = JsonConvert.DeserializeObject<ScheduleAnnouncementActionDetails>(activity.Value.ToString());
            if (details == null || details.Id == null)
            {
                details = JsonConvert.DeserializeObject<TaskModule.BotFrameworkCardValue<ScheduleAnnouncementActionDetails>>
                    (activity.Value.ToString()).Data;
            }
            var dateTime = DateTime.Parse(details.Date + " " + details.Time);
            var offset = activity.LocalTimestamp.Value.Offset;
            DateTimeOffset dateTimeOffset = new DateTimeOffset(dateTime, offset);
            if (campaign.Schedule == null)
                campaign.Schedule = new Models.Schedule()
                {
                    ScheduleId = string.Empty
                };
            campaign.Schedule.ScheduledTime = dateTimeOffset;
            var scheduleDate = campaign.Schedule.GetScheduleTimeUTC(); // Handle timezone differences.
            if (!Scheduler.UpdateSchedule(campaign.Schedule.ScheduleId, scheduleDate))
            {
                campaign.Schedule.ScheduleId = Scheduler.AddSchedule(
                       scheduleDate,
                       new AnnouncementSender()
                       {
                           AnnouncementId = campaign.Id
                       }.Execute);
            }
            campaign.Status = Status.Scheduled;
            await Cache.Announcements.AddOrUpdateItemAsync(campaign.Id, campaign);
        }

        private static async Task SendAnnouncement(IDialogContext context, Activity activity, TeamsChannelData channelData, Campaign campaign)
        {
            await AnnouncementSender.SendAnnouncement(campaign);
        }

        private async Task CreateOrEditAnnouncement(IDialogContext context, Activity activity, TeamsChannelData channelData)
        {
            // Get all the details for announcement.
            var details = JsonConvert.DeserializeObject<TaskModule.BotFrameworkCardValue<CreateNewAnnouncementData>>(activity.Value.ToString());

            // Add announcemnet in DB.
            var campaign = await AddAnnouncementToDB(activity, details.Data, channelData.Tenant.Id);
            bool isEditPeview = true;
            if (details.Data.ActionType == Constants.EditAnnouncementFromTab)
                isEditPeview = false;
            await SendPreviewCard(context, activity, campaign, isEditPeview);

        }

        private static async Task SendPreviewCard(IDialogContext context, Activity activity, Campaign campaign, bool isEditPreview)
        {
            if (campaign.Status == Status.Sent)
            {
                // Send error message if the campaign was already sent
                await context.PostAsync("This announcement is already sent.");
                return;
            }

            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
            var reply = activity.CreateReply();
            reply.Attachments.Add(campaign.GetPreviewCard().ToAttachment());

            // Check if the user is editing a message that is currently being previewed
            PreviewCardMessageDetails previewMessageDetails = null;
            if (isEditPreview)
                previewMessageDetails = context.ConversationData.GetValueOrDefault<PreviewCardMessageDetails>(campaign.Id);

            if (previewMessageDetails == null)
            {
                // Send the preview, and keep track of the message IDs for the card and action buttons
                var previewCardActivity = await connector.Conversations.SendToConversationAsync(reply);

                // Send action buttons.
                reply = activity.CreateReply();
                DateTimeOffset dateTimeOffset = activity.LocalTimestamp.Value.AddHours(1);
                if (campaign.Schedule != null)
                    dateTimeOffset = campaign.Schedule.ScheduledTime;

                reply.Attachments.Add(CardHelper.GetScheduleConfirmationCard(campaign.Id, dateTimeOffset.ToString("MM/dd/yyyy"), dateTimeOffset.ToString("HH:mm"), true));
                var actionCardActivity = await connector.Conversations.SendToConversationAsync(reply);

                // Store information about the preview and actions so that we can update them later
                previewMessageDetails = new PreviewCardMessageDetails()
                {
                    MessageCardId = previewCardActivity.Id,
                    MessageActionId = actionCardActivity.Id
                };

                context.ConversationData.SetValue(campaign.Id, previewMessageDetails);
            }
            else
            {
                // User is editing the message currently being previewed, so just update the current preview
                await connector.Conversations.UpdateActivityAsync(activity.Conversation.Id, previewMessageDetails.MessageCardId, reply);
            }
        }

        private async Task SetModerators(IDialogContext context, Activity activity, TeamsChannelData channelData)
        {
            // TODO: Updated DB
            var details = JsonConvert.DeserializeObject<ModeratorActionDetails>(activity.Value.ToString());
            if (details == null || details.Moderators == null)
            {
                details = JsonConvert.DeserializeObject<TaskModule.BotFrameworkCardValue<ModeratorActionDetails>>
                    (activity.Value.ToString()).Data;
            }
            var moderatorList = details.Moderators.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(m => m.ToLower().Trim()).ToList();
            if (moderatorList.Count == 0)
            {
                await context.PostAsync("Please set at least one moderator.");
                return;
            }
            Tenant tenantData = await Common.CheckAndAddTenantDetails(channelData.Tenant.Id);
            tenantData.Moderators = moderatorList;

            await Cache.Tenants.AddOrUpdateItemAsync(tenantData.Id, tenantData);
            await context.PostAsync("Moderators are set successfully. These users can now create message and post.");
        }

        private async Task SendUpdateGroupConfigurationCard(IDialogContext context, Activity activity, TeamsChannelData channelData)
        {
            var configurationCard = CardHelper.GetGroupConfigurationCard();

            var reply = activity.CreateReply();
            reply.Attachments.Add(configurationCard.ToAttachment());
            await context.PostAsync(reply);
        }

        private async Task SendGrantAdminConsentCard(IDialogContext context, Activity activity, TeamsChannelData channelData)
        {
            var configurationCard = new ThumbnailCard
            {
                Text = "Please grant permission to the application first."
            };

            // Show button with Open URl.
            AdminUserDetails adminDetails = new AdminUserDetails
            {
                ServiceUrl = activity.ServiceUrl,
                UserEmailId = await GetCurrentUserEmailId(activity)
            };
            var loginUrl = GetAdminConsentUrl(channelData.Tenant.Id, ApplicationSettings.AppId, adminDetails);
            configurationCard.Buttons = new List<CardAction>();
            configurationCard.Buttons.Add(new CardAction()
            {
                Title = "Grant Admin Permission",
                Value = loginUrl,
                Type = ActionTypes.OpenUrl
            });

            var reply = activity.CreateReply();
            reply.Attachments.Add(configurationCard.ToAttachment());
            await context.PostAsync(reply);
        }

        private static string GetAdminConsentUrl(string tenant, string appId, AdminUserDetails adminDetails)
        {
            var data = System.Web.HttpUtility.UrlEncode(JsonConvert.SerializeObject(adminDetails));
            return $"https://login.microsoftonline.com/{tenant}/adminconsent?client_id={appId}&state={data}&redirect_uri={ System.Web.HttpUtility.UrlEncode(ApplicationSettings.BaseUrl + "/adminconsent")}";
        }

        private async Task<Campaign> AddAnnouncementToDB(Activity activity, CreateNewAnnouncementData data, string tenantId)
        {
            Campaign announcement = new Campaign()
            {
                IsAcknowledgementRequested = bool.Parse(data.Acknowledge),
                IsContactAllowed = bool.Parse(data.AllowContactIns),
                ShowAllDetailsButton = true,
                Title = data.Title,
                SubTitle = data.SubTitle,
                CreatedTime = DateTime.Now,
                Author = new Author()
                {
                    EmailId = data.AuthorAlias
                },
                Preview = data.Preview,
                Body = data.Body,
                ImageUrl = data.Image,
                Sensitivity = (MessageSensitivity)Enum.Parse(typeof(MessageSensitivity), data.MessageType),
                OwnerId = await GetCurrentUserEmailId(activity)
            };

            announcement.Id = string.IsNullOrEmpty(data.Id) ? Guid.NewGuid().ToString() : data.Id; // Assing the Existing Announcement Id
            announcement.TenantId = tenantId;

            if (!string.IsNullOrEmpty(data.Id))
            {
                var dbAnnouncement = await Cache.Announcements.GetItemAsync(data.Id);
                if (dbAnnouncement.Status == Status.Scheduled)
                {
                    announcement.Schedule = dbAnnouncement.Schedule;
                    Scheduler.RemoveSchedule(announcement.Schedule.ScheduleId);// Clear the schedule
                    // Remove schedule
                }
            }
            var recipients = new RecipientInfo
            {
                ServiceUrl = activity.ServiceUrl,
                TenantId = tenantId
            };

            if (data.Channels != null)
            {
                var channels = data.Channels.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var channelInfo in channels)
                {
                    var info = channelInfo.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    var teamId = info[0];
                    var channelId = info[1];
                    var team = await Cache.Teams.GetItemAsync(teamId);
                    recipients.Channels.Add(new ChannelRecipient()
                    {
                        TeamId = teamId,
                        Channel = new RecipientDetails()
                        {
                            Id = channelId,
                        },
                        Members = team?.Members
                    });
                }
            }

            if (data.Groups != null)
            {
                var groups = data.Groups.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var groupId in groups)
                {
                    var groupDetailsFromDB = await Cache.Groups.GetItemAsync(groupId);
                    var recipientGroup = new GroupRecipient()
                    {
                        GroupId = groupId
                    };
                    foreach (var userEmailId in groupDetailsFromDB.Users)
                    {
                        recipientGroup.Users.Add(new RecipientDetails()
                        {
                            Id = userEmailId,
                        });
                    }
                    recipients.Groups.Add(recipientGroup);
                }
            }
            announcement.Recipients = recipients;
            announcement.Status = Status.Draft;

            var tenantDetails = await Cache.Tenants.GetItemAsync(tenantId);
            // Fetch author Email ID
            if (tenantDetails.IsAdminConsented)
            {
                var token = await GraphHelper.GetAccessToken(tenantId, ApplicationSettings.AppId, ApplicationSettings.AppSecret);
                GraphHelper helper = new GraphHelper(token);
                var userDetails = await helper.GetUser(announcement.Author.EmailId);
                if (userDetails != null)
                {
                    announcement.Author.Name = userDetails.DisplayName;
                    announcement.Author.Role = userDetails.JobTitle ?? userDetails.UserPrincipalName;
                    announcement.Author.ProfilePhoto = await helper.GetUserProfilePhoto(tenantId, userDetails.Id);
                }
                else
                {
                    announcement.Author.Name = announcement.Author.EmailId;
                    announcement.Author.ProfilePhoto = ApplicationSettings.BaseUrl + "/Resources/Person.png";
                }
            }

            if (string.IsNullOrEmpty(data.Id))
            {
                await Cache.Announcements.AddOrUpdateItemAsync(announcement.Id, announcement);// Push to DB
                tenantDetails.Announcements.Add(announcement.Id);
                await Cache.Tenants.AddOrUpdateItemAsync(tenantDetails.Id, tenantDetails); // Update tenat catalog
            }
            else
                await Cache.Announcements.AddOrUpdateItemAsync(data.Id, announcement);// Push updated data DB

            return announcement;
        }

        #endregion

        #region Helpers

        private static async Task ClearCache(IDialogContext context)
        {
            Cache.Announcements = new DBCache<Campaign>();
            Cache.Groups = new DBCache<Group>();
            Cache.Teams = new DBCache<Team>();
            Cache.Tenants = new DBCache<Tenant>();
            Cache.Users = new DBCache<User>();

            await context.PostAsync("Cache cleared.");
        }

        private async Task RefreshProfilePhotos(IDialogContext context, Activity activity, Tenant tenant, User userDetails)
        {
            if (!tenant.IsAdminConsented)
                return;

            var token = await GraphHelper.GetAccessToken(tenant.Id, ApplicationSettings.AppId, ApplicationSettings.AppSecret);
            GraphHelper helper = new GraphHelper(token);
            foreach (var userId in tenant.Users)
            {
                var userInfo = await helper.GetUser(userId);
                if (userInfo != null)
                    await helper.GetUserProfilePhoto(tenant.Id, userInfo.Id);
            }
            await context.PostAsync("Profile photos are refreshed.");
        }

        internal async static Task<User> CheckAndAddUserDetails(Activity activity, TeamsChannelData channelData)
        {
            var currentUser = await GetCurrentUser(activity);
            if (currentUser == null)
                return null;
            // User not present in cache
            var userDetails = await Cache.Users.GetItemAsync(currentUser.UserPrincipalName.ToLower());
            if (userDetails == null && currentUser != null)
            {
                userDetails = new User()
                {
                    BotConversationId = activity.From.Id,
                    Id = currentUser.UserPrincipalName.ToLower(),
                    Name = currentUser.Name ?? currentUser.GivenName,
                    AadObjectId = currentUser.AadObjectId

                };
                await Cache.Users.AddOrUpdateItemAsync(userDetails.Id, userDetails);

                Tenant tenantData = await Common.CheckAndAddTenantDetails(channelData.Tenant.Id);
                if (!tenantData.Users.Contains(userDetails.Id))
                {
                    tenantData.Users.Add(userDetails.Id);
                    await Cache.Tenants.AddOrUpdateItemAsync(tenantData.Id, tenantData);
                }
            }

            return userDetails;
        }

        /// <summary>
        ///  Process the Excel file attachment with information about user groups
        /// </summary>
        private static async Task HandleExcelAttachement(IDialogContext context, Attachment attachment, TeamsChannelData channelData)
        {
            if (attachment.ContentType == FileDownloadInfo.ContentType)
            {
                FileDownloadInfo downloadInfo = (attachment.Content as JObject).ToObject<FileDownloadInfo>();
                var filePath = System.Web.Hosting.HostingEnvironment.MapPath("~/Files/");
                if (!Directory.Exists(filePath))
                    Directory.CreateDirectory(filePath);

                filePath += attachment.Name + DateTime.Now.Millisecond; // just to avoid name collision with other users
                if (downloadInfo != null)
                {
                    using (WebClient myWebClient = new WebClient())
                    {
                        // Download the Web resource and save it into the current filesystem folder.
                        myWebClient.DownloadFile(downloadInfo.DownloadUrl, filePath);

                    }
                    if (File.Exists(filePath))
                    {
                        var groupDetails = ExcelHelper.GetAddTeamDetails(filePath);
                        if (groupDetails != null)
                        {
                            // Code to check if DLs are passed in Excel and fetch user lists
                            if (groupDetails.Any(g => g.DistributionLists != null && g.DistributionLists.Count > 0))
                            {
                                var token = await GraphHelper.GetAccessToken(channelData.Tenant.Id, ApplicationSettings.AppId, ApplicationSettings.AppSecret);
                                var graphHelper = new GraphHelper(token);

                                foreach (var group in groupDetails)
                                {
                                    if (group.DistributionLists != null && group.DistributionLists.Count > 0)
                                        foreach (var possibleDL in group.DistributionLists)
                                        {
                                            var DLMembers = await graphHelper.GetAllMembersOfGroup(possibleDL);
                                            group.Users.AddRange(DLMembers.Select(m => Common.RemoveHashFromGuestUserUPN(m.userPrincipalName.ToLower())));
                                            group.Users = group.Users.Distinct().ToList();
                                        }
                                }
                            }
                            var tenantData = await Common.CheckAndAddTenantDetails(channelData.Tenant.Id);

                            await Common.CreateOrUpdateExistingGroups(groupDetails, tenantData, true);

                            var groups = new List<Group>();
                            StringBuilder groupInfoString = new StringBuilder();
                            foreach (var groupId in tenantData.Groups)
                            {
                                var group = await Cache.Groups.GetItemAsync(groupId);
                                if (group != null)
                                {
                                    groupInfoString.Append($"Group Name: **{group.Name}**  User count: **{ group.Users.Count}**\n\n");
                                }
                            }
                            await context.PostAsync($"Successfully updated Group details for your tenant. Here are group details:\n\n" + groupInfoString.ToString());

                            var reply = context.MakeMessage();
                            reply.Attachments.Add(CardHelper.GetWelcomeScreen(channelData.Team != null, Role.Admin));
                            await context.PostAsync(reply);
                        }
                        else
                        {
                            await context.PostAsync($"Attachment received but unfortunately we are not able to read group details. Please make sure that all the colums are correct.");
                        }

                        File.Delete(filePath);
                    }
                }
            }
        }


        private static string GetKey(IActivity activity, string key)
        {
            return activity.From.Id + key;
        }

        public static async Task<string> GetCurrentUserEmailId(Activity activity)
        {
            // Fetch the members in the current conversation
            using (ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl)))
                try
                {
                    var exponentialBackoffRetryStrategy = new ExponentialBackoff(3, TimeSpan.FromSeconds(2),
                           TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(1));

                    // Define the Retry Policy
                    var retryPolicy = new RetryPolicy(new BotSdkTransientExceptionDetectionStrategy(), exponentialBackoffRetryStrategy);

                    var members = await retryPolicy.ExecuteAsync(() =>
                                                connector.Conversations.GetConversationMembersAsync(activity.Conversation.Id)
                                                ).ConfigureAwait(false);

                    if (members.Count == 0)
                        return null;
                    var upn = members.FirstOrDefault(m => m.Id == activity.From.Id)?.AsTeamsChannelAccount()?.UserPrincipalName?.ToLower();
                    return Common.RemoveHashFromGuestUserUPN(upn);
                }
                catch (Exception)
                {
                    // This is the case for compose extension.
                    var channelData = activity.GetChannelData<TeamsChannelData>();
                    var tid = channelData.Tenant.Id;
                    return await GetEmailIdFromGraphAPI(activity, tid);
                }
        }

        private static async Task<string> GetEmailIdFromGraphAPI(Activity activity, string tid)
        {
            var tenant = await Cache.Tenants.GetItemAsync(tid);
            if (tenant == null || !tenant.IsAdminConsented)
                return null;

            var token = await GraphHelper.GetAccessToken(tid, ApplicationSettings.AppId, ApplicationSettings.AppSecret);
            GraphHelper helper = new GraphHelper(token);

            var emailId = await helper.GetUserEmailId(activity.From.AadObjectId);
            return Common.RemoveHashFromGuestUserUPN(emailId);
        }

        public static async Task<TeamsChannelAccount> GetCurrentUser(Activity activity)
        {
            // Fetch the members in the current conversation
            using (ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl)))

            {
                var exponentialBackoffRetryStrategy = new ExponentialBackoff(3, TimeSpan.FromSeconds(2),
                          TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(1));

                // Define the Retry Policy
                var retryPolicy = new RetryPolicy(new BotSdkTransientExceptionDetectionStrategy(), exponentialBackoffRetryStrategy);

                var members = await retryPolicy.ExecuteAsync(() =>
                                             connector.Conversations.GetConversationMembersAsync(activity.Conversation.Id)
                                            ).ConfigureAwait(false);


                // var members = await connector.Conversations.GetConversationMembersAsync(activity.Conversation.Id);
                if (members.Count == 0)
                    return null;
                return members.FirstOrDefault(m => m.Id == activity.From.Id)?.AsTeamsChannelAccount();
            }
        }

        #endregion
    }
}