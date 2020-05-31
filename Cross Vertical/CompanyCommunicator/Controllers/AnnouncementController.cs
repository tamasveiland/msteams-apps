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
using AdaptiveCards.Rendering.Html;
using CrossVertical.Announcement.Helper;
using CrossVertical.Announcement.Helpers;
using CrossVertical.Announcement.Models;
using CrossVertical.Announcement.Repository;
using CrossVertical.Announcement.ViewModels;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.Protocol;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CrossVertical.Announcement.Controllers
{
    public class AnnouncementController : Controller
    {
        #region View Actions

        [Route("")]
        public ActionResult Index()
        {
            // Landing page
            return View();
        }

        /// <summary>
        /// Shows My Messages Tab
        /// </summary>
        [Route("history")]
        public async Task<ActionResult> History(string tid, string emailId)
        {
            if (string.IsNullOrEmpty(tid) || string.IsNullOrEmpty(emailId))
            {
                return HttpNotFound();
            }
            emailId = emailId.ToLower();
            var tenatInfo = await Common.CheckAndAddTenantDetails(tid);
            Role role = Common.GetUserRole(emailId, tenatInfo);
            var myTenantAnnouncements = await Common.GetMyAnnouncements(emailId, tenatInfo.Id);

            HistoryViewModel historyViewModel = new HistoryViewModel();
            historyViewModel.Role = role;
            foreach (var announcement in myTenantAnnouncements.OrderByDescending(a => a.CreatedTime))
            {
                //var campaign = announcement.Post as Campaign;
                PostDetails post = new PostDetails();
                post.Id = announcement.Id;
                post.Title = announcement.Title;
                post.Date = announcement.CreatedTime;
                post.Status = announcement.Status;
                post.MessageSensitivity = announcement.Sensitivity;

                var recipientCount = 0;
                var groupsNames = new List<string>();
                var channelNames = new List<string>();

                // Get all group recipients
                foreach (var group in announcement.Recipients.Groups)
                {
                    var groupname = await Cache.Groups.GetItemAsync(group.GroupId);
                    if (groupname == null)
                        continue;

                    groupsNames.Add(groupname.Name);
                    recipientCount += group.Users.Count;
                    post.LikeCount += group.Users.Sum(u => u.LikeCount);
                    post.AckCount += group.Users.Where(u => u.IsAcknoledged).Count();
                }
                foreach (var team in announcement.Recipients.Channels)
                {
                    var teamname = await Cache.Teams.GetItemAsync(team.TeamId);
                    if (teamname == null)
                        continue;
                    if (!channelNames.Contains(teamname.Name))
                    {
                        channelNames.Add(teamname.Name);

                        post.RecipientChannelCount += teamname.Members.Count;
                    }

                    post.LikeCount += team.LikedUsers.Count;
                }
                if (recipientCount == 0 && announcement.Recipients != null && announcement.Recipients.Channels != null)
                    recipientCount = announcement.Recipients.Channels.Count;

                var maxChar = 40;
                var recipientNames = string.Empty;
                var recipientChannelNames = string.Empty;
                for (int i = 0; i < groupsNames.Count; i++)
                {
                    if (i != 0)
                        recipientNames += ", ";
                    recipientNames += groupsNames[i];

                    if (recipientNames.Length >= maxChar)
                    {
                        // Check the actual count
                        recipientNames += " +" + (groupsNames.Count - i);
                        break;
                    }

                }
                for (int i = 0; i < channelNames.Count; i++)
                {
                    if (i != 0)
                        recipientNames += ", ";
                    recipientChannelNames += channelNames[i];

                    if (recipientNames.Length >= maxChar)
                    {
                        // Check the actual count
                        recipientChannelNames += " +" + (channelNames.Count - i);
                        break;
                    }
                }
                post.RecipientCount = $"{recipientCount}";
                post.Recipients = $"{recipientNames}";
                post.RecipientChannelNames = $"{recipientChannelNames}";

                historyViewModel.Posts.Add(post);
            }

            return View(historyViewModel);
        }


        [Route("details")]
        public async Task<ActionResult> Details(string announcementid)
        {
            AdaptiveCardRenderer renderer = new AdaptiveCardRenderer();
            if (string.IsNullOrEmpty(announcementid))
            {
                return HttpNotFound();
            }

            var announcement = await Cache.Announcements.GetItemAsync(announcementid);
            AnnouncementDetails announcementinfo = new AnnouncementDetails();
            if (announcement != null)
            {
                announcement.ShowAllDetailsButton = false;
                var html = announcement.GetPreviewCard();
                announcement.ShowAllDetailsButton = true;
                RenderedAdaptiveCard renderedCard = renderer.RenderCard(html);
                HtmlTag cardhtml = renderedCard.Html;

                announcementinfo.Title = announcement.Title;
                announcementinfo.html = cardhtml;

            }
            return View(announcementinfo);
        }

        [Route("viewAnalytics")]
        public async Task<ActionResult> ViewAnalytics(string id, string tid, string page)
        {
            var announcement = await Cache.Announcements.GetItemAsync(id);
            TabListViewModel analyticsInfo = new TabListViewModel();
            analyticsInfo.FirstTab.Title = "Acknowledgement";
            analyticsInfo.SecondTab.Title = "Reaction";
            analyticsInfo.FirstTab.TenantId = analyticsInfo.SecondTab.TenantId = tid;

            analyticsInfo.FirstTab.Type = "personAcknowledgement";
            analyticsInfo.SecondTab.Type = "personReaction";

            // Fill in analyticsInfo model.
            if (announcement != null)
            {

                var token = await GraphHelper.GetAccessToken(tid, ApplicationSettings.AppId, ApplicationSettings.AppSecret);
                GraphHelper helper = new GraphHelper(token);

                var allAckUsers = new List<string>();
                var allReactionUsers = new List<string>();
                foreach (var group in announcement.Recipients.Groups)
                {
                    foreach (var user in group.Users)
                    {
                        // Fill in the Item details.
                        var userDetails = await Cache.Users.GetItemAsync(user.Id);

                        if (user.IsAcknoledged)
                        {
                            if (!allAckUsers.Contains(user.Id))
                            {
                                var item = GetUserItem(tid, helper, userDetails);
                                analyticsInfo.FirstTab.Items.Add(item);
                                allAckUsers.Add(user.Id);
                            }
                        }
                        if (user.LikeCount != 0)
                        {
                            if (!allReactionUsers.Contains(user.Id))
                            {
                                var item = GetUserItem(tid, helper, userDetails);
                                item.EnableLikeButton = true;
                                analyticsInfo.SecondTab.Items.Add(item);
                                allReactionUsers.Add(user.Id);
                            }
                        }
                    }
                }
                foreach (var channel in announcement.Recipients.Channels)
                {
                    foreach (var user in channel.LikedUsers)
                    {
                        //var userDetails = await Cache.Users.GetItemAsync(user.Id);
                        var userDetails = await Cache.Users.GetItemAsync(user);
                        if (channel.LikedUsers.Count != 0)
                        {
                            if (!allReactionUsers.Contains(user))
                            {
                                var item = GetUserItem(tid, helper, userDetails);
                                item.EnableLikeButton = true;
                                analyticsInfo.SecondTab.Items.Add(item);
                                allReactionUsers.Add(user);
                            }
                        }
                    }
                }
            }

            analyticsInfo.SelectFirstTab = page == "viewAckAnalytics";
            if (analyticsInfo.SelectFirstTab && analyticsInfo.FirstTab.Items.Count == 0)
                analyticsInfo.SelectFirstTab = false;
            return View("TabListView", analyticsInfo);
        }

        private static ViewModels.Item GetUserItem(string tid, GraphHelper helper, User userDetails)
        {
            // Add this in view.
            return new ViewModels.Item()
            {
                Id = userDetails.Id,
                ImageUrl = ApplicationSettings.BaseUrl + "/Resources/Person.png", //await helper.GetUserProfilePhoto(tid, userDetails.Id),
                Title = userDetails.Name,
                SubTitle = "",
                EnableLikeButton = false,
                ChatUrl = $"https://teams.microsoft.com/l/chat/0/0?users={userDetails.Id}"
                // ChatUrl = $"https://teams.microsoft.com/_#/conversations/8:orgid:{userDetails.Id}?ctx=chat"
            };
        }

        [Route("viewAudiance")]
        public async Task<ActionResult> ViewAudiance(string id, string tid, string page)
        {
            var announcement = await Cache.Announcements.GetItemAsync(id);
            TabListViewModel audianceInfo = new TabListViewModel();
            audianceInfo.FirstTab.Title = "1:1 Chat";
            audianceInfo.FirstTab.Type = "personAudiance";

            audianceInfo.SecondTab.Title = "Channels";
            audianceInfo.SecondTab.Type = "channelAudiance";

            audianceInfo.FirstTab.TenantId = audianceInfo.SecondTab.TenantId = tid;
            audianceInfo.SelectFirstTab = page == "viewGroupAudiance";
            // Fill in audianceInfo model.
            if (announcement != null)
            {
                var token = await GraphHelper.GetAccessToken(tid, ApplicationSettings.AppId, ApplicationSettings.AppSecret);
                GraphHelper helper = new GraphHelper(token);

                var allUsers = new List<string>();
                foreach (var group in announcement.Recipients.Groups)
                {
                    foreach (var user in group.Users)
                    {
                        if (allUsers.Contains(user.Id))
                        {
                            continue;
                        }
                        else
                            allUsers.Add(user.Id);

                        var userDetails = await Cache.Users.GetItemAsync(user.Id);
                        if (userDetails == null)
                            continue;
                        var item = GetUserItem(tid, helper, userDetails);
                        audianceInfo.FirstTab.Items.Add(item);
                    }
                }

                foreach (var channel in announcement.Recipients.Channels)
                {
                    var messageId = channel.Channel.MessageId;
                    var team = await Cache.Teams.GetItemAsync(channel.TeamId);
                    if (team == null)
                        continue;

                    var channelDetails = team.Channels.FirstOrDefault(c => c.Id == channel.Channel.Id);
                    if (channelDetails == null)
                        continue;

                    audianceInfo.SecondTab.Items.Add(
                         new ViewModels.Item()
                         {
                             Id = team.AadObjectId,
                             ImageUrl = ApplicationSettings.BaseUrl + "/Resources/Team.png",
                             Title = channelDetails.Name,
                             SubTitle = team.Name,
                             EnableLikeButton = false,
                             DeepLinkUrl =
                             !string.IsNullOrEmpty(messageId) ?
                             "https://teams.microsoft.com/l/message/" + messageId?.Replace(";messageid=", "/")
                             : $"https://teams.microsoft.com/l/channel/{channel.TeamId}/General"
                         });
                }
            }
            return View("TabListView", audianceInfo);
        }

        [Route("fetchProfilePhoto")]
        public async Task<JObject> FetchProfilePhoto(string tid, string id, string type)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(tid))
                return null;

            var userProfile = id.Contains("/");
            var userId = userProfile ? id.Split('/').Last() : id;

            var token = await GraphHelper.GetAccessToken(tid, ApplicationSettings.AppId, ApplicationSettings.AppSecret);
            GraphHelper helper = new GraphHelper(token);
            ViewModels.Item item = null;
            if (type.Contains("person"))
            {
                var user = await helper.GetUser(userId);
                var photo = await helper.GetUserProfilePhoto(tid, user.Id);

                item = new ViewModels.Item()
                {
                    Id = userId,
                    ImageUrl = photo,
                    Title = user.DisplayName ?? user.GivenName,
                    SubTitle = user.JobTitle ?? user.UserPrincipalName,
                };
            }
            else if (type == "channelAudiance")
            {
                var teamId = Team.GetTeamId(userId);
                var photo = await helper.GetTeamPhoto(tid, userId);
                item =
                    new ViewModels.Item()
                    {
                        Id = userId,
                        ImageUrl = photo
                    };

            }
            return JObject.FromObject(item);
        }

        [Route("CleanDatabase")]
        [HttpPost]
        public async Task<ActionResult> CleanDatabase()
        {
            var authRequestHeader = Request.Headers["AuthKey"];
            if (!string.IsNullOrEmpty(authRequestHeader) && ConfigurationManager.AppSettings["AuthKey"] == authRequestHeader)
            {

                // Clean up Schedules
                await AnnouncementScheduler.CleanUpOldSchedules();
                // Cleanup up database
                await DocumentDBRepository.CleanUpAsync();
                // Cleanup cached.
                Cache.Clear();
                // Delete profile photos
                var baseDirectory = System.Web.Hosting.HostingEnvironment.MapPath($"~/ProfilePhotos/");
                if (System.IO.Directory.Exists(baseDirectory))
                {
                    // Delete directory and then create it.
                    System.IO.Directory.Delete(baseDirectory, true);
                    System.IO.Directory.CreateDirectory(baseDirectory);
                }

                // Cleanup TableStorage
                var storageAccountConnectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
                var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
                var tableClient = storageAccount.CreateCloudTableClient();
                var table = tableClient.GetTableReference("botdata");
                await table.DeleteIfExistsAsync();

                // Recreate DB Collection
                await DocumentDBRepository.Initialize();
                await SafeCreateIfNotExists(table);
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.OK);
            }
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden, "Auth key does not match.");
        }

        public static async Task<bool> SafeCreateIfNotExists(CloudTable table, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            do
            {
                try
                {
                    return table.CreateIfNotExists(requestOptions, operationContext);
                }
                catch (StorageException e)
                {
                    if ((e.RequestInformation.HttpStatusCode == 409) && (e.RequestInformation.ExtendedErrorInformation.ErrorCode.Equals(TableErrorCodeStrings.TableBeingDeleted)))
                        await Task.Delay(2000);// The table is currently being deleted. Try again until it works.
                    else
                        throw;
                }
            } while (true);
        }

        #endregion

        #region Task Module Cards

        [Route("getCreateNewCard")]
        public async Task<JObject> GetCreateNewCard(string tid)
        {
            return JObject.FromObject(await CardHelper.GetCreateNewAnnouncementCard(tid));
        }

        [Route("getEditCard")]
        public async Task<JObject> GetEditCard(string id, string tid)
        {
            return JObject.FromObject(await CardHelper.GetEditAnnouncementCardForTab(id, tid));
        }

        [Route("getTemplateCard")]
        public async Task<JObject> GetTemplateCard(string id, string tid)
        {
            return JObject.FromObject(await CardHelper.GetTemplateCard(id, tid));
        }

        [Route("getModifyScheduleCard")]
        public async Task<JObject> GetModifyScheduleCard(string id)
        {
            return JObject.FromObject(await CardHelper.GetScheduleConfirmationCard(id));
        }

        [Route("getPreviewCard")]
        public async Task<JObject> GetPreviewCard(string id)
        {
            return JObject.FromObject(await CardHelper.GetPreviewAnnouncementCard(id));
        }

        #endregion

        #region local testing

        [Route("listviewtest")]
        public ActionResult TestListView()
        {
            var viewModel = new ListItemsViewModel();
            viewModel.ListItems = new List<ListItem>()
            {
                new ListItem(){
                    ImageUrl ="https://pbs.twimg.com/profile_images/3647943215/d7f12830b3c17a5a9e4afcc370e3a37e_400x400.jpeg",
                    Title ="Niana Seril",
                    SubTitle ="Senior Manager",
                    EnableLikeButton = true,
                    ChatUrl = "https://pbs.twimg.com/profile_images/3647943215/d7f12830b3c17a5a9e4afcc370e3a37e_400x400.jpeg"
                },


                new ListItem(){ImageUrl="https://pbs.twimg.com/profile_images/3647943215/d7f12830b3c17a5a9e4afcc370e3a37e_400x400.jpeg",
                    Title ="My ChanelName",
                    SubTitle ="Team Name",
                    DeepLinkUrl = "https://pbs.twimg.com/profile_images/3647943215/d7f12830b3c17a5a9e4afcc370e3a37e_400x400.jpeg"
                },


                new ListItem(){
                    ImageUrl ="https://pbs.twimg.com/profile_images/3647943215/d7f12830b3c17a5a9e4afcc370e3a37e_400x400.jpeg",
                    Title ="Antonio Clausen",
                    SubTitle ="Sales Manager",
                ChatUrl="https://pbs.twimg.com/profile_images/3647943215/d7f12830b3c17a5a9e4afcc370e3a37e_400x400.jpeg",
                EnableLikeButton=false,
                },


                new ListItem(){
                    ImageUrl ="https://pbs.twimg.com/profile_images/3647943215/d7f12830b3c17a5a9e4afcc370e3a37e_400x400.jpeg",
                    Title ="Omar Mast",
                    SubTitle ="Solution Specialist",
                EnableLikeButton = true,
                    ChatUrl = "https://pbs.twimg.com/profile_images/3647943215/d7f12830b3c17a5a9e4afcc370e3a37e_400x400.jpeg"},


                new ListItem(){
                    ImageUrl ="https://pbs.twimg.com/profile_images/3647943215/d7f12830b3c17a5a9e4afcc370e3a37e_400x400.jpeg",
                    Title ="Nelson Morales",
                    SubTitle ="Chief Business Operator",
                    DeepLinkUrl = "https://pbs.twimg.com/profile_images/3647943215/d7f12830b3c17a5a9e4afcc370e3a37e_400x400.jpeg"

                },

        };
            return View(viewModel);
        }

        [Route("testcreate")]
        public async Task<ActionResult> Create(string Emailid)
        {
            User user1 = new User() { BotConversationId = "id1", Id = "user@microsoft.com", Name = "User 1" };
            User user2 = new User() { BotConversationId = "id2", Id = "user2@microsoft.com", Name = "User 2" };
            User user3 = new User() { BotConversationId = "id3", Id = "user3@microsoft.com", Name = "User 3" };
            Group group1 = new Group()
            {
                Id = "GroupId1",
                Name = "Group 1",
                Users = new List<string>() { user1.Id, user2.Id }
            };
            Group group2 = new Group()
            {
                Id = "GroupId2",
                Name = "Group 2",
                Users = new List<string>() { user2.Id, user3.Id }
            };

            Team team1 = new Team()
            {
                Id = "Team1",
                Name = "Team1",
                Channels = new List<Channel>()
                {
                    new Channel() { Id = "channel1", Name = "Channel 1" },
                    new Channel() { Id = "channel2", Name = "Channel 2" },
                    new Channel() { Id = "channel3", Name = "Channel 3" },
                }
            };

            Team team2 = new Team()
            {
                Id = "Team2",
                Name = "Team2",
                Channels = new List<Channel>()
                {
                    new Channel() { Id = "channel1", Name = "Channel 1" },
                    new Channel() { Id = "channel2", Name = "Channel 2" },
                    new Channel() { Id = "channel3", Name = "Channel 3" },
                }
            };

            Tenant tenant = new Tenant()
            {
                Id = "Tenant1",
                Groups = new List<string>() { group1.Id, group2.Id },
                Users = new List<string>() { user1.Id, user2.Id, user3.Id },
                Teams = new List<string>() { team1.Id, team2.Id }
            };

            Campaign campaignAnnouncement = new Campaign()
            {
                IsAcknowledgementRequested = true,
                IsContactAllowed = true,
                Title = "Giving Campaign 2018 is here",
                SubTitle = "Have you contributed to the mission?",
                Author = new Author()
                {
                    Name = "John Doe",
                    Role = "Director of Corporate Communications",
                },
                Preview = "The 2018 Employee Giving Campaign is officially underway! Our incredibly generous culture of employee giving is unique to Contoso, and has a long history going back to our founder and his family’s core belief and value in philanthropy. Individually and collectively, we can have an incredible impact no matter how we choose to give. We are all very fortunate and 2018 has been a good year for the company which we are all participating in. Having us live in a community with a strong social safety net benefits us all so lets reflect our participation in this year's success with participation in Give.",
                Body = "The 2018 Employee Giving Campaign is officially underway! Our incredibly generous culture of employee giving is unique to Contoso, and has a long history going back to our founder and his family’s core belief and value in philanthropy. Individually and collectively, we can have an incredible impact no matter how we choose to give. We are all very fortunate and 2018 has been a good year for the company which we are all participating in. Having us live in a community with a strong social safety net benefits us all so lets reflect our participation in this year's success with participation in Give. I hope you will take advantage of some of the fun and impactful opportunities that our giving team has put together and I’d like to thank our VPALs John Doe and Jason Natie for all the hard work they've put into planning these events for our team. To find out more about these opportunities, look for details in Give 2018",
                ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSG-vkjeuIlD-up_-VHCKgcREhFGp27lDErFkveBLQBoPZOHwMbjw",
                Sensitivity = MessageSensitivity.Information
            };

            campaignAnnouncement.Id = "Announcement3";
            campaignAnnouncement.TenantId = tenant.Id;

            var recipients = new RecipientInfo();
            recipients.Channels.Add(new ChannelRecipient()
            {
                TeamId = team1.Id,
                Channel = new RecipientDetails()
                {
                    Id = "channel1",
                }
            });

            recipients.Channels.Add(new ChannelRecipient()
            {
                TeamId = team2.Id,
                Channel = new RecipientDetails()
                {
                    Id = "channel2",
                }
            });

            recipients.Groups.Add(new GroupRecipient()
            {
                GroupId = group1.Id,
                Users = new List<RecipientDetails>() {
                    new RecipientDetails()
                    {
                        Id = user1.Id,
                    },
                    new RecipientDetails()
                    {
                        Id = user2.Id,
                    },
                }
            });
            campaignAnnouncement.Recipients = recipients;
            campaignAnnouncement.Status = Status.Draft;

            // Insert
            //await DocumentDBRepository.CreateItemAsync(user1);
            //await DocumentDBRepository.CreateItemAsync(user2);

            // Udpate
            //user1.Name += "Updated";
            //await DocumentDBRepository.UpdateItemAsync(user1.EmailId, user1);

            //await DocumentDBRepository.CreateItemAsync(group1);
            //await DocumentDBRepository.CreateItemAsync(group2);

            //await DocumentDBRepository.CreateItemAsync(team1);
            //await DocumentDBRepository.CreateItemAsync(team2);

            //await DocumentDBRepository.CreateItemAsync(tenant);

            await DocumentDBRepository.CreateItemAsync(campaignAnnouncement);

            // Update announcements.
            tenant.Announcements.Add(campaignAnnouncement.Id);
            await DocumentDBRepository.UpdateItemAsync(campaignAnnouncement.Id, campaignAnnouncement);

            var allGroups = await DocumentDBRepository.GetItemsAsync<Group>(u => u.Type == nameof(Group));
            var allTeam = await DocumentDBRepository.GetItemsAsync<Team>(u => u.Type == nameof(Team));
            var allTenants = await DocumentDBRepository.GetItemsAsync<Tenant>(u => u.Type == nameof(Tenant));
            var allAnnouncements = await DocumentDBRepository.GetItemsAsync<Campaign>(u => u.Type == nameof(Campaign));
            var myTenantAnnouncements = allAnnouncements.Where(a => a.TenantId == tenant.Id);
            return View();
        }

        #endregion
    }
}
