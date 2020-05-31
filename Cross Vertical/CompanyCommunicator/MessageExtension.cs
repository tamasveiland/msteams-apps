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
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace CrossVertical.Announcement
{
    public class MessageExtension
    {
        public async static Task<ComposeExtensionResponse> HandleMessageExtensionQuery(ConnectorClient connector, Activity activity, string tid, string emailId)
        {
            var query = activity.GetComposeExtensionQueryData();
            if (query == null || query.CommandId != "getMyMessages")
            {
                // We only process the 'getMyMessages' queries with this message extension
                return null;
            }

            var searchString = string.Empty;
            var titleParam = query.Parameters?.FirstOrDefault(p => p.Name == "title");
            if (titleParam != null)
            {
                searchString = titleParam.Value.ToString().ToLower();
            }
            var attachments = new List<ComposeExtensionAttachment>();
            var announcements = await GetMyAnnouncements(tid, emailId, searchString);
            foreach (var item in announcements.OrderByDescending(a => a.CreatedTime))
            {
                var attachment2 = GetAttachment(item);
                attachments.Add(attachment2);
            }
            var response = new ComposeExtensionResponse(new ComposeExtensionResult("list", "result"));
            response.ComposeExtension.Attachments = attachments.ToList();
            return response;
        }

        public static async Task<List<Campaign>> GetMyAnnouncements(string tid, string emailId, string searchString)
        {
            var tenatInfo = await Cache.Tenants.GetItemAsync(tid);
            var myTenantAnnouncements = new List<Campaign>();
            emailId = emailId.ToLower();
            var myAnnouncements = tenatInfo.Announcements;
            Role role = Common.GetUserRole(emailId, tenatInfo);
            foreach (var announcementId in myAnnouncements)
            {
                var announcement = await Cache.Announcements.GetItemAsync(announcementId);
                if (announcement != null)
                {
                    var authorName = announcement.Author?.Name?.ToLower() ?? "";
                    if (!announcement.Title.ToLower().Contains(searchString)
                        && !announcement.SubTitle.ToLower().Contains(searchString)
                        && !(authorName.Contains(searchString)))
                    {
                        continue;
                    }
                    if (role == Role.Moderator || role == Role.Admin)
                    {
                        myTenantAnnouncements.Add(announcement);
                    }
                    else if (announcement.Recipients.Channels.Any(c => c.Members.Contains(emailId))
                          || announcement.Recipients.Groups.Any(g => g.Users.Any(u => u.Id == emailId)))
                    {
                        // Validate if user is part of this announcement.
                        myTenantAnnouncements.Add(announcement);
                    }
                }
            }
            return myTenantAnnouncements;
        }

        private static ComposeExtensionAttachment GetAttachment(Campaign campaign)
        {
            var previewCard = new ThumbnailCard
            {
                Title = campaign.Title,
                Text = campaign.SubTitle,
            };
            previewCard.Images = new List<CardImage>() {
                new CardImage(Uri.IsWellFormedUriString(campaign.Author?.ProfilePhoto, UriKind.Absolute) ?
                campaign.Author?.ProfilePhoto : ApplicationSettings.BaseUrl + "/Resources/Person.png" ) };
            campaign.ShowAllDetailsButton = false;
            var card = campaign.GetPreviewCard().ToAttachment();
            campaign.ShowAllDetailsButton = true;
            return card
                .ToComposeExtensionAttachment(previewCard.ToAttachment());
        }
    }

}