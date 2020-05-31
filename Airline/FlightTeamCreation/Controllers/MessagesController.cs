using Airline.FlightTeamCreation.Web;
using Airline.FlightTeamCreation.Web.Models;
using Airline.FlightTeamCreation.Web.Repository;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace Airline.FlightTeamCreation.Web.Controllers
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {

        string GraphRootUri = ConfigurationManager.AppSettings["GraphRootUri"];

        /// <summary>
        /// POST: api/Messages
        /// receive a message from a user and send replies
        /// </summary>
        /// <param name="activity"></param>
        [ResponseType(typeof(void))]
        public virtual async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            // check if activity is of type message
            if (activity != null && activity.GetActivityType() == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new RootDialog());
            }
            else if (activity.IsO365ConnectorCardActionQuery())
            {
                // this will handle the request coming any action on Actionable messages
                return await HandleO365ConnectorCardActionQuery(activity);
            }
            else if (activity != null && activity.GetActivityType() == ActivityTypes.Invoke && activity.Name == "signin/verifyState")
            {
                var stateInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<SateValue>(activity.Value.ToString());

                // var userInfo =
                // userData.SetProperty<string>("AccessToken", activity.Value.ToString());
                UserInfoRepository.AddUserInfo(new UserInfo() { userId = activity.From.Id, accessToken = stateInfo.state, ExpiryTime = DateTime.Now.AddSeconds(3450) }); //3450

                var reply = activity.CreateReply();
                reply.Text = "You are successfully signed in. Now, you can use the 'create team' command.";


                // Get Email Id.
                var connectorClient = new ConnectorClient(new Uri(activity.ServiceUrl));
                var email = string.Empty;
                var member = connectorClient.Conversations.GetConversationMembersAsync(activity.Conversation.Id).Result.AsTeamsChannelAccounts().FirstOrDefault();
                if (member != null)
                    email = member.Email;


                var card = RootDialog.GetFilter(email);
                reply.Attachments.Add(card);

                
                await connectorClient.Conversations.ReplyToActivityAsync(reply);


            }
            else
            {
                HandleSystemMessage(activity);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }

        private async Task<HttpResponseMessage> HandleO365ConnectorCardActionQuery(Activity activity)
        {
            var connectorClient = new ConnectorClient(new Uri(activity.ServiceUrl));

            var userInfo = UserInfoRepository.GetUserInfo(activity.From.Id);

            // Validate for Sing In
            if (userInfo == null || userInfo.ExpiryTime < DateTime.Now)
            {
                var reply = activity.CreateReply();
                SigninCard plCard = RootDialog.GetSignInCard();
                reply.Attachments.Add(plCard.ToAttachment());
                await connectorClient.Conversations.ReplyToActivityWithRetriesAsync(reply);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }

            var email = string.Empty;
            var member = connectorClient.Conversations.GetConversationMembersAsync(activity.Conversation.Id).Result.AsTeamsChannelAccounts().FirstOrDefault();
            if (member != null)
                email = member.Email;


            // Get O365 connector card query data.
            Task<Task> task = new Task<Task>(async () =>
            {

                O365ConnectorCardActionQuery o365CardQuery = activity.GetO365ConnectorCardActionQueryData();
                Activity replyActivity = activity.CreateReply();
                switch (o365CardQuery.ActionId)
                {
                    case "Custom":
                        // Get Passenger List & Name
                        var teamDetails = Newtonsoft.Json.JsonConvert.DeserializeObject<CustomTeamData>(o365CardQuery.Body);
                        await CreateTeam(connectorClient, activity, userInfo, teamDetails.TeamName, teamDetails.Members.Split(';').ToList());
                        break;
                    case "Flight":
                        var flightDetails = Newtonsoft.Json.JsonConvert.DeserializeObject<O365BodyValue>(o365CardQuery.Body);


                        await CreateTeam(connectorClient, activity, userInfo, "Flight-" + flightDetails.Value, GetMemberList(email));
                        // await AttachClassWisePassengerList(classInfo.Value, replyActivity, $"Passengers with {classInfo.Value} tickets");
                        break;

                    default:
                        break;
                }


            });
            task.Start();

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }

        public static List<string> GetMemberList(string email)
        {
            if (email.ToLower().Contains("@microsoft.com") || email.ToLower().Contains("@teamsdevtest.onmicrosoft.com"))
                return HardcodedMembersListForMicrosoftTenants;
            return
                HardcodedMembersListDefault;
        }

        // These are hardcoded for demo tenant.
        public static List<string> HardcodedMembersListForMicrosoftTenants = new List<string>() {
                    "Myrtle@teamsdevtest.onmicrosoft.com",
                    "Tomba@teamsdevtest.onmicrosoft.com",
                    "Maxima@teamsdevtest.onmicrosoft.com"
                    };

        // These are hardcoded for demo tenant.
        public static List<string> HardcodedMembersListDefault = new List<string>() {"IrvinS@M365x614055.onmicrosoft.com",
                    "MeganB@M365x614055.onmicrosoft.com",
                    "MiriamG@M365x614055.onmicrosoft.com",
                    "NestorW@M365x614055.onmicrosoft.com",
                    "EnriqueB@M365x614055.onmicrosoft.com",
                    "AmyP@M365x614055.onmicrosoft.com",
                    "RubyG@M365x614055.onmicrosoft.com" };

        private async Task CreateTeam(ConnectorClient connector, Activity activity, UserInfo userInfo, string teamName, List<string> members)
        {
            var groupId = await CreateGroupAsyn(userInfo.accessToken, teamName);
            if (IsValidGuid(groupId))
            {
                await ReplyWithMessage(activity, connector, $"Created O365 group for '{teamName}'. Now, adding Microsoft Teams. This may take some time.");

                var retryCount = 4;
                string teamId = null;
                while (retryCount > 0)
                {
                    teamId = await CreateTeamAsyn(userInfo.accessToken, groupId);
                    if (IsValidGuid(teamId))
                    {
                        await ReplyWithMessage(activity, connector, $" '{teamName}' Team created successfully.");
                        break;
                    }
                    else
                    {
                        teamId = null;
                    }
                    retryCount--;
                    await Task.Delay(9000);
                }

                if (teamId != null)
                {
                    var channelId1 = await CreateChannel(userInfo.accessToken, teamId, "Gate Operations and Passenger Boarding",
                                        "Gate Operations and Passenger Boarding channel");
                    var channelId2 = await CreateChannel(userInfo.accessToken, teamId, "Ground Crew", "Ground Crew channel");
                    var channelId3 = await CreateChannel(userInfo.accessToken, teamId, "Onboard Crew", "Onboard Crew channel");
                    var channelId4 = await CreateChannel(userInfo.accessToken, teamId, "Reservation Agencies", "Reservation Agencies channel");

                    // Add users:
                    foreach (var member in members)
                    {
                        var memberEmailId = member.Trim();
                        var result = await AddUserToTeam(userInfo, teamId, memberEmailId);
                        if (!result)
                            await ReplyWithMessage(activity, connector, $"Failed to add {memberEmailId} to team.");
                    }

                    await ReplyWithMessage(activity, connector, $"Channels, Members Added successfully. Process Completed.");
                }
                else
                {
                    await ReplyWithMessage(activity, connector, $"Failed to create team due to internal error. Please try again later.");
                }

            }
        }

        private Activity HandleSystemMessage(Activity message)
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

        private static async Task ReplyWithMessage(Activity activity, ConnectorClient connector, string message)
        {
            var reply = activity.CreateReply();
            reply.Text = message;
            await connector.Conversations.ReplyToActivityAsync(reply);
        }

        private async Task<bool> AddUserToTeam(UserInfo userInfo, string teamId, string userEmailId)
        {
            var userId = await GetUserId(userInfo.accessToken, userEmailId);
            return await AddTeamMemberAsync(userInfo.accessToken, teamId, userId);
        }

        bool IsValidGuid(string guid)
        {
            Guid teamGUID;
            return Guid.TryParse(guid, out teamGUID);
        }


        public async Task<string> CreateChannel(
          string accessToken, string teamId, string channelName, string channelDescription)
        {
            string endpoint = GraphRootUri + $"groups/{teamId}/team/channels";

            ChannelInfoBody channelInfo = new ChannelInfoBody()
            {
                description = channelDescription,
                displayName = channelName
            };

            return await PostRequest(accessToken, endpoint, JsonConvert.SerializeObject(channelInfo));
        }


        public async Task<string> CreateGroupAsyn(
            string accessToken, string groupName)
        {
            string endpoint = GraphRootUri + "groups/";

            GroupInfo groupInfo = new GroupInfo()
            {
                description = "Team for " + groupName,
                displayName = groupName,
                groupTypes = new string[] { "Unified" },
                mailEnabled = true,
                mailNickname = groupName.Replace(" ", "") + DateTime.Now.Second,
                securityEnabled = true
            };

            return await PostRequest(accessToken, endpoint, JsonConvert.SerializeObject(groupInfo));
        }


        public async Task<bool> AddTeamMemberAsync(
            string accessToken, string teamId, string userId)
        {
            string endpoint = GraphRootUri + $"groups/{teamId}/members/$ref";

            var userData = $"{{ \"@odata.id\": \"https://graph.microsoft.com/beta/directoryObjects/{userId}\" }}";

            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    request.Content = new StringContent(userData, Encoding.UTF8, "application/json");

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {

                            return true;
                        }
                        return false;
                    }
                }
            }

        }


        public async Task<string> CreateTeamAsyn(
           string accessToken, string groupId)
        {
            // This might need Retries.
            string endpoint = GraphRootUri + $"groups/{groupId}/team";

            TeamInfoData teamInfo = new TeamInfoData()
            {
                funSettings = new Funsettings() { allowGiphy = true, giphyContentRating = "strict" },
                messagingSettings = new Messagingsettings() { allowUserEditMessages = true, allowUserDeleteMessages = true },
                memberSettings = new Membersettings() { allowCreateUpdateChannels = true }

            };
            return await PutRequest(accessToken, endpoint, JsonConvert.SerializeObject(teamInfo));
        }


        private static async Task<string> PostRequest(string accessToken, string endpoint, string groupInfo)
        {
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    request.Content = new StringContent(groupInfo, Encoding.UTF8, "application/json");

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {

                            var createdGroupInfo = JsonConvert.DeserializeObject<ResponseData>(response.Content.ReadAsStringAsync().Result);
                            return createdGroupInfo.id;
                        }
                        return null;
                    }
                }
            }

        }


        private static async Task<string> PutRequest(string accessToken, string endpoint, string groupInfo)
        {
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Put, endpoint))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    request.Content = new StringContent(groupInfo, Encoding.UTF8, "application/json");

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {

                            var createdGroupInfo = JsonConvert.DeserializeObject<ResponseData>(response.Content.ReadAsStringAsync().Result);
                            return createdGroupInfo.id;
                        }
                        return null;
                    }
                }
            }
        }


        /// <summary>
        /// Get the current user's id from their profile.
        /// </summary>
        /// <param name="accessToken">Access token to validate user</param>
        /// <returns></returns>
        public async Task<string> GetUserId(string accessToken, string userEmailId)
        {
            string endpoint = GraphRootUri + $"users/{userEmailId}";
            string queryParameter = "?$select=id";

            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, endpoint + queryParameter))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    string userId = "";
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                            userId = json.GetValue("id").ToString();
                        }
                        return userId?.Trim();
                    }
                }
            }
        }
    }

    public class SateValue
    {
        public string state { get; set; }
    }
}