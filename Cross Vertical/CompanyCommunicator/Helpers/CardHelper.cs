﻿// Copyright (c) Microsoft. All rights reserved.
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
using AdaptiveCards;
using CrossVertical.Announcement.Helper;
using CrossVertical.Announcement.Models;
using CrossVertical.Announcement.Repository;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskModule;

namespace CrossVertical.Announcement.Helpers
{
    /// <summary>
    /// Contains all the Adpative card designs
    /// </summary>
    public class CardHelper
    {

        public static Attachment GetWelcomeScreen(bool isChannelCard, Role role)
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveContainer()
                    {
                        Items=new List<AdaptiveElement>()
                        {
                            new AdaptiveImage()
                            {
                                Url=new System.Uri(ApplicationSettings.BaseUrl + "/Resources/Announce-Header.png")
                            },
                            new AdaptiveTextBlock()
                            {
                                Weight=AdaptiveTextWeight.Bolder,
                                Text= role == Role.User?
                                "Get contacted where you collaborate."
                                :"Reach people right where they collaborate. "
                            },
                            new AdaptiveTextBlock()
                            {
                                IsSubtle=true,
                                Text= role == Role.User?
                                $"Never again miss a formal communication of interest from your enterprise on Microsoft Teams! Get notified about instant messages sent to you or your team, react, acknowledge, track and view engagement on relevant messages - all in a structured manner within your favorite workspace.\n Using this app, you can:"
                                : $"Get the message out to employees using Microsoft Teams. Send {ApplicationSettings.AppFeature} to a set of employees, stores, roles or locations in one or more channels or individually.\nUsing this app, you can:",
                                Wrap=true
                            },
                            new AdaptiveTextBlock()
                            {
                                Size=AdaptiveTextSize.Small,
                                IsSubtle=true,
                                Wrap=true,
                                Text= role == Role.User?
                                $"* Receive communication sent to large employee groups\n* Get notified about {ApplicationSettings.AppFeature}s sent to you via 1:1 chat\n* Get messages in channels to encourage discussion and feedback\n* See {ApplicationSettings.AppFeature}s to desktop, web clients or mobile clients of Microsoft Teams  – wherever users are\n* Track and report engagement on messages you receive\n* Send “read receipt or acknowledgments, if requested"
                                :$"* Collaborate and communicate with large employee groups\n* Target {ApplicationSettings.AppFeature} via 1:1 chats for select employees\n* Post in Channels to encourage discussion and feedback\n* Deliver {ApplicationSettings.AppFeature}s to desktop, web clients or mobile clients of Microsoft Teams  – wherever users are\n* Track and report employee engagement on what you post\n* Track and report employee’s “read receipt if requested "
                            },
                            new AdaptiveTextBlock()
                            {
                                Text= isChannelCard ? "Note: This application works only in personal scope." : "Take your pick to get started:",
                                IsSubtle=true,

                            }
                        }
                    }
                }
            };
            if (isChannelCard)
            {
                card.Actions.Add(new AdaptiveOpenUrlAction()
                {
                    Id = "chatinpersonal",
                    Title = "Go to Personal App",
                    Url = new System.Uri($"https://teams.microsoft.com/l/chat/0/0?users=28:{ApplicationSettings.AppId}")
                });
            }

            else if (role == Role.User)
                card.Actions = new List<AdaptiveAction>()
                {
                        new AdaptiveSubmitAction()
                        {
                            Id="showdrafts",
                            Title="View Recents",
                            Data = new ActionDetails() { ActionType = Constants.ShowRecents}
                        },
                        new AdaptiveOpenUrlAction()
                        {
                            Id="viewall",
                            Title="📄 View All", // Take to Tab
                            Url = new System.Uri(Constants.HistoryTabDeeplink)
                        }
                };
            else
                card.Actions = new List<AdaptiveAction>()
                {
                    new AdaptiveSubmitAction()
                    {
                        Id = "createmessage",
                        Title = "📢 Create Message",
                        Data = new AdaptiveCardValue<ActionDetails>()
                        { Data = new ActionDetails() { ActionType = Constants.CreateOrEditAnnouncement } }
                    },
                        new AdaptiveSubmitAction()
                        {
                            Id="showdrafts",
                            Title="⏱️ View Drafts & Scheduled",
                            Data = new ActionDetails() { ActionType = Constants.ShowAllDrafts}
                        },
                        new AdaptiveOpenUrlAction()
                        {
                            Id="viewall",
                            Title="📄 View All", // Take to Tab
                            Url = new System.Uri(Constants.HistoryTabDeeplink)
                        },
                        new AdaptiveSubmitAction()
                        {
                            Id = "adminpanel",
                            Title = "⚙️ Admin Panel",
                            Data = new ActionDetails() { ActionType = Constants.ConfigureAdminSettings }
                        }
                };
            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
        }

        public static Attachment GetCardForNonConsentedTenant()
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveContainer()
                    {
                        Items=new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock()
                            {
                                Text= "Currently this app is not configured by your administrator in your organization. If you are admin then please configure the app from personal scrope.",
                                IsSubtle=true,
                                Wrap = true
                            }
                        }
                    }
                }
            };
            card.Actions.Add(new AdaptiveOpenUrlAction()
            {
                Id = "chatinpersonal",
                Title = "Go to Personal App",
                Url = new System.Uri($"https://teams.microsoft.com/l/chat/0/0?users=28:{ApplicationSettings.AppId}")
            });

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
        }

        public static async Task<Attachment> GetEditAnnouncementCard(string announcementId, string tenantId)
        {
            var campaign = await Cache.Announcements.GetItemAsync(announcementId);

            var tenant = await Cache.Tenants.GetItemAsync(tenantId);

            var groups = new List<Group>();
            foreach (var groupID in tenant.Groups)
            {
                groups.Add(await Cache.Groups.GetItemAsync(groupID));
            }

            var teams = new List<Team>();
            foreach (var teamID in tenant.Teams)
            {
                teams.Add(await Cache.Teams.GetItemAsync(teamID));
            }

            return campaign.GetCreateNewCard(groups, teams, true).ToAttachment();
        }

        public static async Task<Attachment> GetEditAnnouncementCardForTab(string announcementId, string tenantId)
        {
            var card = await GetEditAnnouncementCard(announcementId, tenantId);
            return SetSubmitAction(card, new AnnouncementActionDetails()
            {
                ActionType = Constants.EditAnnouncementFromTab,
                Id = announcementId
            });
        }

        private static Attachment SetSubmitAction(Attachment card, ActionDetails action)
        {
            var campaign = card.Content as AdaptiveCard;
            var submitAction = campaign.Actions.FirstOrDefault();
            if (submitAction != null && submitAction.Title == "✔️ Preview")// TODO: change this
            {
                var acknowledgeAction = submitAction as AdaptiveSubmitAction;
                acknowledgeAction.Data = action;
            }
            return card;
        }

        public static async Task<Attachment> GetTemplateCard(string announcementId, string tenantId)
        {
            var card = await GetEditAnnouncementCard(announcementId, tenantId);

            return SetSubmitAction(card, new ActionDetails() { ActionType = Constants.CreateOrEditAnnouncement });
        }

        public static async Task<Attachment> GetPreviewAnnouncementCard(string announcementId)
        {
            var campaign = await Cache.Announcements.GetItemAsync(announcementId);

            return GetPreviewAnnouncementCard(campaign);
        }

        public static Attachment GetPreviewAnnouncementCard(Campaign campaign)
        {
            campaign.ShowAllDetailsButton = false;
            var card = campaign.GetPreviewCard().ToAttachment();
            campaign.ShowAllDetailsButton = true;
            return card;
        }

        public static async Task<Attachment> GetCreateNewAnnouncementCard(string tenantId)
        {
            var tenant = await Cache.Tenants.GetItemAsync(tenantId);

            var groups = new List<Group>();
            foreach (var groupID in tenant.Groups)
            {
                var group = await Cache.Groups.GetItemAsync(groupID);
                if (group != null)
                    groups.Add(group);
            }

            var teams = new List<Team>();
            foreach (var teamID in tenant.Teams)
            {
                var team = await Cache.Teams.GetItemAsync(teamID);
                if (team != null)
                    teams.Add(team);
            }
            return new Campaign().GetCreateNewCard(groups, teams, false).ToAttachment();
        }

        public static async Task<Attachment> GetScheduleConfirmationCard(string announcementId)
        {
            var campaign = await Cache.Announcements.GetItemAsync(announcementId);
            var date = campaign.Schedule.ScheduledTime.ToString("MM/dd/yyyy");
            var time = campaign.Schedule.ScheduledTime.ToString("HH:mm");

            var card = new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveContainer()
                                        {
                                            Items=new List<AdaptiveElement>()
                                            {
                                                new AdaptiveTextBlock()
                                                {
                                                    Text=$"Please select schedule for your {ApplicationSettings.AppFeature}:"
                                                },
                                                new AdaptiveDateInput()
                                                {
                                                    Id = "Date",
                                                    Value = date
                                                },
                                                new AdaptiveTimeInput()
                                                {
                                                    Id = "Time",
                                                    Value = time
                                                }
                                            }
                                        }
                },
                Actions = new List<AdaptiveAction>()
                          {
                            new AdaptiveSubmitAction()
                            {
                                Id = "sendNow",
                                Title = "Send Now",
                                Data = new AnnouncementActionDetails()
                                {
                                    ActionType = Constants.SendAnnouncement,
                                    Id = announcementId
                                }
                            },
                            new AdaptiveSubmitAction()
                            {
                                Id= "schedule",
                                Title="Schedule",
                                Data = new AnnouncementActionDetails()
                                {
                                    Id = announcementId,
                                    ActionType = Constants.ScheduleAnnouncement
                                }
                            }
                          }
            };

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
        }

        public static Attachment GetAdminPanelCard(string currentModerators)
        {
            var Card = new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveContainer()
                    {
                        Items=new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock()
                            {
                                Weight=AdaptiveTextWeight.Bolder,
                                Text="Please select your action:"
                            }
                        }
                    }
                },
                Actions = new List<AdaptiveAction>()
                          {
                    new AdaptiveShowCardAction(){
                         Id = "configureGroups",
                         Title = "Configure Groups",
                         Card=new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
                                {
                                    Body=new List<AdaptiveElement>()
                                    {
                                        new AdaptiveContainer()
                                        {
                                            Items=new List<AdaptiveElement>()
                                            {
                                                new AdaptiveTextBlock()
                                                {
                                                    Text=$"This option allows you to create logical group of employees which can be selected as audience while seding {ApplicationSettings.AppFeature}s:",
                                                    Wrap = true

                                                },
                                                new AdaptiveImage()
                                                {
                                                     Url=new System.Uri(ApplicationSettings.BaseUrl + "/Resources/SelectGroups.png")
                                                }
                                            }
                                        }
                                    },
                                    Actions=new List<AdaptiveAction>()
                                    {
                                      new AdaptiveShowCardAction(){
                                      Id = "configureGroups",
                                      Title = "From Excel",
                                      Card=new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
                                      {
                                        Body=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveContainer()
                                            {
                                                Items=new List<AdaptiveElement>()
                                                {
                                                    new AdaptiveTextBlock()
                                                    {
                                                        Text=$"This allows you to upload an excel with Group Name, Users or Distribution Lists to create logical group for App to send {ApplicationSettings.AppFeature}s.\n\nExample:",
                                                        Wrap = true

                                                    },
                                                    new AdaptiveImage()
                                                    {
                                                         Url=new System.Uri(ApplicationSettings.BaseUrl + "/Resources/ConfigureGroups.png")
                                                    }
                                                }
                                            }
                                        },
                                        Actions=new List<AdaptiveAction>()
                                        {
                                          new AdaptiveSubmitAction()
                                          {
                                              Id= "configureGroups",
                                              Title="Upload Excel",
                                              Data =  new ActionDetails()
                                                {
                                                    ActionType = Constants.ConfigureGroups,
                                                }
                                          }
                                        }
                                        }
                                    },
                                      new AdaptiveShowCardAction(){
                                    Id = "createGroupWithAllEmployees",
                                    Title = "From Azure AD",
                                    Card=new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
                                    {
                                        Body=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveContainer()
                                            {
                                                Items=new List<AdaptiveElement>()
                                                {
                                                    new AdaptiveTextBlock()
                                                    {
                                                        Text=$"This will fetch all the users present in Azure Active Directory and create logical group of name \"All Employees\" which can be selected as audiance to send {ApplicationSettings.AppFeature}s",
                                                        Wrap = true
                                                    }
                                                }
                                            }
                                        },
                                    Actions=new List<AdaptiveAction>()
                                    {
                                      new AdaptiveSubmitAction()
                                      {
                                            Id = "createGroupWithAllEmployees",
                                            Title = "Create/Update Group",
                                            Data = new ActionDetails()
                                            {
                                                ActionType = Constants.CreateGroupWithAllEmployees,
                                            }
                                      }
                                    }
                                }
                            }
                                    }
                                }
                            },
                            new AdaptiveShowCardAction()
                            {
                                Id = "setModerators",
                                Title="Set Moderators",
                                Card=new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
                                {
                                    Body=new List<AdaptiveElement>()
                                    {
                                        new AdaptiveContainer()
                                        {
                                            Items=new List<AdaptiveElement>()
                                            {
                                                new AdaptiveTextBlock()
                                                {
                                                    Text=$"Please set list of moderators who gets option to create and send {ApplicationSettings.AppFeature} . ",
                                                    Wrap  = true

                                                },
                                                new AdaptiveTextInput()
                                                {
                                                    Id = "Moderators",
                                                    Placeholder="ex: user1@org.com, user2@org.com",
                                                    Value = currentModerators
                                                }
                                            }
                                        }
                                    },
                                    Actions=new List<AdaptiveAction>()
                                    {
                                      new AdaptiveSubmitAction()
                                      {
                                          Id= "setModerator",
                                          Title=string.IsNullOrEmpty(currentModerators) ?"Set" : "Updated",
                                          Data = new ModeratorActionDetails(){  ActionType = Constants.SetModerators}
                                      }
                                    }
                                }
                              },
                            new AdaptiveShowCardAction(){
                                            Id = "createTeamsWithAllEmployees",
                                            Title = "Create Team of All Employees",
                             Card=new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
                                {
                                    Body=new List<AdaptiveElement>()
                                    {
                                        new AdaptiveContainer()
                                        {
                                            Items=new List<AdaptiveElement>()
                                            {
                                                new AdaptiveTextBlock()
                                                {
                                                    Text=$"This creates one or more new teams with of All Employees in tenant fetched from Azure Active Directory. This is a workaround to get app installed for all the employees present in tenant. Once this step is completed, please wait for all the members to sync in Teams and then install {ApplicationSettings.AppName} in newly created team.",
                                                    Wrap = true
                                                }
                                            }
                                        }
                                    },
                                    Actions=new List<AdaptiveAction>()
                                    {
                                      new AdaptiveSubmitAction()
                                        {
                                            Id = "createTeamsWithAllEmployees",
                                            Title = "Create",
                                            Data = new ActionDetails()
                                            {
                                                ActionType = Constants.CreateTeamsWithAllEmployees,
                                            }
                                        }
                                    }
                                }
                            }

                          }
            };

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = Card
            };
        }

        public static Attachment GetScheduleConfirmationCard(string announcementId, string date, string time, bool allowEdit)
        {
            var Card = new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveContainer()
                    {
                        Items=new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock()
                            {
                                Weight=AdaptiveTextWeight.Bolder,
                                Text="Please select your action:"
                            }
                        }
                    }
                },
                Actions = new List<AdaptiveAction>()
                          {
                            new AdaptiveSubmitAction()
                            {
                                Id = "sendNow",
                                Title = "Send Now",
                                Data = new AnnouncementActionDetails()
                                {
                                    ActionType = Constants.SendAnnouncement ,
                                    Id = announcementId
                                }
                            },
                            new AdaptiveShowCardAction()
                            {
                                Id = "sendLater",
                                Title="Send Later",
                                Card=new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
                                {
                                    Body=new List<AdaptiveElement>()
                                    {
                                        new AdaptiveContainer()
                                        {
                                            Items=new List<AdaptiveElement>()
                                            {
                                                new AdaptiveTextBlock()
                                                {
                                                    Text=$"Schedule your {ApplicationSettings.AppFeature} here"
                                                },
                                                new AdaptiveDateInput()
                                                {
                                                    Id = "Date",
                                                    Placeholder="Select Date",
                                                    Value = date
                                                },
                                                new AdaptiveTimeInput()
                                                {
                                                    Id = "Time",
                                                    Placeholder="Select time",
                                                    Value = time
                                                }
                                            }
                                        }
                                    },
                                    Actions=new List<AdaptiveAction>()
                                    {
                                      new AdaptiveSubmitAction()
                                      {
                                          Id= "schedule",
                                          Title="Schedule",
                                          Data = new AnnouncementActionDetails(){ Id = announcementId,  ActionType = Constants.ScheduleAnnouncement}
                                      }
                                    }
                                }
                              }

                          }
            };

            if (allowEdit)
            {
                Card.Actions.Add(
                new AdaptiveSubmitAction()
                {
                    Id = "editAnnouncement",
                    Title = "Edit",
                    Data = new AdaptiveCardValue<AnnouncementActionDetails>()
                    {
                        Data = new AnnouncementActionDetails()
                        {
                            ActionType = Constants.ShowEditAnnouncementTaskModule,
                            Id = announcementId
                        }
                    }
                });
            }

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = Card
            };
        }

        public static Attachment GetUpdateMessageCard(string message)
        {
            var Card = new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveContainer()
                    {
                        Items=new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock()
                            {
                                Weight=AdaptiveTextWeight.Bolder,
                                Text=message,
                                Wrap = true
                            }
                        }
                    }
                }
            };

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = Card
            };
        }

        public static Attachment GetCardWithAcknowledgementDetails(Attachment campaignAttachment, string id, string userId, string groupId)
        {
            var campaign = campaignAttachment.Content as AdaptiveCard;
            var action = campaign.Actions.FirstOrDefault(a => a.Title == Constants.Acknowledge);
            if (action != null)
            {
                var acknowledgeAction = action as AdaptiveSubmitAction;
                acknowledgeAction.Data = new AnnouncementAcknowledgeActionDetails()
                {
                    ActionType = Constants.Acknowledge,
                    Id = id,
                    GroupId = groupId,
                    UserId = userId
                };
            }
            return campaignAttachment;
        }

        public static Attachment GetCardToUpdatePreviewCard(Attachment campaignAttachment, string message)
        {

            var campaign = campaignAttachment.Content as AdaptiveCard;

            campaign.Body.Add(
                new AdaptiveContainer()
                {
                    Items = new List<AdaptiveElement>()
                                {
                                    new AdaptiveTextBlock()
                                    {
                                        Text= message,
                                        Wrap=true,
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Left,
                                        Spacing=AdaptiveSpacing.None,
                                        Weight=AdaptiveTextWeight.Bolder,
                                        Color= AdaptiveTextColor.Attention,
                                        MaxLines=1
                                    }
                                }
                });
            return campaignAttachment;
        }

        public static Attachment GetCardWithoutAcknowledgementAction(Attachment campaignAttachment)
        {
            var campaign = campaignAttachment.Content as AdaptiveCard;
            RemoveAction(campaign, Constants.Acknowledge);
            RemoveAction(campaign, Constants.ContactSender);
            return campaignAttachment;
        }

        private static void RemoveAction(AdaptiveCard campaign, string actionName)
        {
            var action = campaign.Actions.FirstOrDefault(a => a.Title == actionName);
            if (action != null)
            {
                campaign.Actions.Remove(action);
            }
        }

        public static Attachment GetAnnouncementBasicDetails(Campaign campaign)
        {
            var basicDetailsCard = new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveColumnSet()
                            {
                                Columns=new List<AdaptiveColumn>()
                                {
                                    new AdaptiveColumn()
                                    {
                                         Width=AdaptiveColumnWidth.Auto,
                                         Items=new List<AdaptiveElement>()
                                         {
                                             // Need to fetch this from Graph API.
                                             new AdaptiveImage(){
                                                 Id = "profileImage",
                                                 Url =  Uri.IsWellFormedUriString(campaign.Author?.ProfilePhoto,UriKind.Absolute)?
                                                 new Uri(campaign.Author?.ProfilePhoto) : null,
                                                 Size =AdaptiveImageSize.Medium,Style=AdaptiveImageStyle.Person }

                                         }
                                    },
                                    new AdaptiveColumn()
                                    {
                                         Width=AdaptiveColumnWidth.Auto,
                                         Items=new List<AdaptiveElement>()
                                         {
                                             new AdaptiveTextBlock(){
                                                 Text = campaign.Title,
                                                 Weight =AdaptiveTextWeight.Bolder,Wrap=true},
                                             new AdaptiveTextBlock(){
                                                 Text =  "Author: " + campaign.Author?.Name,
                                                 Size = AdaptiveTextSize.Default,Spacing=AdaptiveSpacing.None,IsSubtle=true,Wrap=true
                                             },
                                              new AdaptiveTextBlock(){
                                                 Text =  $"Created Date: {campaign.CreatedTime.ToShortDateString()}",
                                                 Weight= AdaptiveTextWeight.Lighter,
                                                 Size = AdaptiveTextSize.Default,Wrap=true}
                                         }
                                    }
                                }
                            }
                },
            };
            return basicDetailsCard.ToAttachment();
        }

        public static ThumbnailCard GetGroupConfigurationCard()
        {
            return new ThumbnailCard
            {
                Text = @"Please go ahead and upload the excel file with Group details in following format:  
                        <ol>
                        <li><strong>Group Name</strong>: String eg: <pre>All Employees</pre></li>
                        <li><strong>Members</strong>  : Comma separated user emails eg: <pre>user1@org.com, user2@org.com</pre></li>
                        <li><strong>Distribution Lists</strong>  : Comma separated distribution list name or email eg: <pre>All External Staff, EmployeeInSeattle@org.com</pre></li></ol>
                        </br> <strong>Note: Please keep first row header as described above. You can provide details for multiple teams row by row. Users/Distribution Lists columns can be empty.</strong>",
            };
        }
    }
}