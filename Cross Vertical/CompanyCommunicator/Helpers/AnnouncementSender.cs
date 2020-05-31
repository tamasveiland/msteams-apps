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
using CrossVertical.Announcement.Models;
using CrossVertical.Announcement.Repository;
using Microsoft.Bot.Connector;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CrossVertical.Announcement.Helpers
{
    public class AnnouncementSender
    {
        public string AnnouncementId { get; set; }

        public async Task Execute()
        {
            var campaign = await Cache.Announcements.GetItemAsync(AnnouncementId);
            if (campaign == null)
                return;

            await SendAnnouncement(campaign);
        }

        public static async Task<bool> SendAnnouncement(Models.Campaign campaign)
        {
            int duplicateUsers = 0;
            var serviceURL = campaign.Recipients.ServiceUrl;
            var tenantId = campaign.Recipients.TenantId;
            var owner = await Cache.Users.GetItemAsync(campaign.OwnerId);
            var taskList = new List<Task>();
            var usersToNotify = new ConcurrentBag<PersonalMessageRecipients>();
            var appNotInstalledUsers = new List<string>();

            foreach (var group in campaign.Recipients.Groups)
            {
                //foreach (var recipient in group.Users)
                await Common.ForEachAsync(group.Users, ApplicationSettings.NoOfParallelTasks,
                  async recipient =>
                  {
                      var user = await Cache.Users.GetItemAsync(recipient.Id);
                      if (user == null)
                      {
                          recipient.FailureMessage = "App not installed";
                          appNotInstalledUsers.Add(recipient.Id);
                      }
                      else
                      {
                          if (usersToNotify.Any(g => g.RecipientDetails.Id == recipient.Id))
                          {
                              // Remove check for temporary
                              recipient.FailureMessage = "Duplicated. Message already sent.";
                              duplicateUsers++;
                              // continue;
                              return;
                          }
                          else
                          {
                              usersToNotify.Add(new PersonalMessageRecipients()
                              {
                                  GroupId = group.GroupId,
                                  UserDetails = user,
                                  RecipientDetails = recipient
                              });
                          }
                      }
                  });
            }

            if (appNotInstalledUsers.Count > 0)
            {
                await ProactiveMessageHelper.SendPersonalNotification(serviceURL, tenantId, owner, $"App not installed by {appNotInstalledUsers.Count} users.", null);
            }

            if (usersToNotify.Count > 100)
            {
                await ProactiveMessageHelper.SendPersonalNotification(serviceURL, tenantId, owner, $"We are sending this message to {usersToNotify.Count} which may take some time. Will notify you once process is completed.", null);
            }

            var messageSendResults = new ConcurrentBag<NotificationSendStatus>();
            await Common.ForEachAsync(usersToNotify, ApplicationSettings.NoOfParallelTasks,
                async recipient =>
                {
                    var user = recipient.UserDetails;
                    var card = campaign.GetPreviewCard().ToAttachment();
                    var campaignCard = CardHelper.GetCardWithAcknowledgementDetails(card, campaign.Id, user.Id, recipient.GroupId);
                    var result = await ProactiveMessageHelper.SendPersonalNotification(serviceURL, tenantId, user, null, card);
                    result.Name = user.Name; // Addd name of use for reporting.
                    if (result.IsSuccessful)
                    {
                        recipient.RecipientDetails.MessageId = result.MessageId;
                    }
                    else
                    {
                        recipient.RecipientDetails.FailureMessage = result.FailureMessage;
                    }
                    messageSendResults.Add(result);
                });

            await Common.ForEachAsync(campaign.Recipients.Channels, ApplicationSettings.NoOfParallelTasks,
               async recipient =>
                {
                    var team = await Cache.Teams.GetItemAsync(recipient.TeamId);
                    if (team == null)
                    {
                        recipient.Channel.FailureMessage = "App not installed";
                        messageSendResults.Add(new NotificationSendStatus()
                        {
                            IsSuccessful = false,
                            FailureMessage = "App not installed",
                            Name = recipient.Channel.Id
                        });
                    }
                    else
                    {
                        var botAccount = new ChannelAccount(ApplicationSettings.AppId);
                        var card = campaign.GetPreviewCard().ToAttachment();
                        var campaignCard = CardHelper.GetCardWithoutAcknowledgementAction(card);

                        var result = await ProactiveMessageHelper.SendChannelNotification(botAccount, serviceURL, recipient.Channel.Id, null, card);
                        result.Name = team.Name;
                        if (result.IsSuccessful)
                        {
                            recipient.Channel.MessageId = result.MessageId;
                        }
                        else
                        {
                            recipient.Channel.FailureMessage = result.FailureMessage;
                        }
                        messageSendResults.Add(result);
                    }
                });

            var failedUsers = string.Join(",", messageSendResults.Where(m => !m.IsSuccessful).Select(m => m.Name).ToArray());
            var successCount = messageSendResults.Count(m => m.IsSuccessful);

            await ProactiveMessageHelper.SendPersonalNotification(serviceURL, tenantId, owner,
                                        $"Process completed. Success: {successCount}. " +
                                        $"Failure: {messageSendResults.Count - successCount }. Duplicate: {duplicateUsers}", null);

            campaign.Status = Status.Sent;
            await Cache.Announcements.AddOrUpdateItemAsync(campaign.Id, campaign);
            return true;
        }

    }
}