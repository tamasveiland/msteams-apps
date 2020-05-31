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
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CrossVertical.Announcement.Helpers
{
    public class GraphHelper
    {
        private readonly string _token;

        public GraphHelper(string token)
        {
            _token = token;
        }

        public static async Task<string> GetAccessToken(string tenant, string appId, string appSecret)
        {
            string response = await POST($"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token",
                              $"grant_type=client_credentials&client_id={appId}&client_secret={appSecret}"
                              + "&scope=https%3A%2F%2Fgraph.microsoft.com%2F.default");

            string accessToken = JsonConvert.DeserializeObject<TokenResponse>(response).access_token;
            return accessToken;
        }

        // Get a specified user's photo.
        public async Task<User> GetUserFromDisplayName(string userName)
        {
            var graphClient = GetAuthenticatedClient();
            // Get the user.
            try
            {
                var filteredUsers = await graphClient.Users.Request()
                .Filter($"startswith(displayName,'{userName}') or startswith(givenName,'{userName}') or startswith(surname,'{userName}') or startswith(mail,'{userName}') or startswith(userPrincipalName,'{userName}')")
                .GetAsync();
                return filteredUsers.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

            }

            return null;
        }

        public async Task<string> GetUserEmailId(string userId)
        {
            var graphClient = GetAuthenticatedClient();
            // Get the user.
            try
            {
                var filteredUsers = await graphClient.Users[userId].Request().GetAsync();
                return filteredUsers?.UserPrincipalName.ToLower();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        public async Task<User> GetUser(string emailId)
        {
            var graphClient = GetAuthenticatedClient();
            // Get the user.
            try
            {
                var filteredUsers = await graphClient.Users.Request()
                .Filter($"startswith(userPrincipalName,'{emailId}')")
                .GetAsync();
                return filteredUsers.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

            }

            return null;
        }

        public async Task<Group> GetGroup(string groupName)
        {
            var graphClient = GetAuthenticatedClient();
            // Get the user.
            try
            {
                var searchParameter = groupName.Contains("@") ? "mail" : "displayName";
                var filteredGroups = await graphClient.Groups.Request()
                    .Filter($"startsWith({searchParameter},'{groupName}')")
                    .GetAsync();
                return filteredGroups.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        public async Task<List<UserDetail>> GetAllMembersOfGroup(string groupName)
        {
            var listOfMembers = new List<UserDetail>();
            var group = await GetGroup(groupName);
            if (group == null)
                return listOfMembers;

            return await FetchAllGroupMembersAsync(group.Id, null);
        }

        public async Task<string> GetUserProfilePhoto(string tenantId, string userId)
        {
            var graphClient = GetAuthenticatedClient();
            var profilePhotoUrl = string.Empty;
            try
            {
                var baseDirectory = $"/ProfilePhotos/{tenantId}/";
                var fileName = userId + ".png";
                string imagePath = System.Web.Hosting.HostingEnvironment.MapPath("~" + baseDirectory);
                if (!System.IO.Directory.Exists(imagePath))
                    System.IO.Directory.CreateDirectory(imagePath);
                imagePath += fileName;

                if (System.IO.File.Exists(imagePath))
                    return ApplicationSettings.BaseUrl + baseDirectory + fileName;

                var photo = await graphClient.Users[userId].Photo.Content.Request().GetAsync();
                using (var fileStream = System.IO.File.Create(imagePath))
                {
                    photo.Seek(0, SeekOrigin.Begin);
                    photo.CopyTo(fileStream);
                }
                profilePhotoUrl = ApplicationSettings.BaseUrl + baseDirectory + fileName;
            }
            catch (Exception ex)
            {
                ErrorLogService.LogError(ex);
                profilePhotoUrl = ApplicationSettings.BaseUrl + "/Resources/Person.png";
            }
            return profilePhotoUrl;
        }

        public async Task<string> GetTeamPhoto(string tenantId, string teamId)
        {
            var graphClient = GetAuthenticatedClient();
            var profilePhotoUrl = string.Empty;
            try
            {
                var baseDirectory = $"/ProfilePhotos/{tenantId}/";
                var fileName = teamId + ".png";
                string imagePath = System.Web.Hosting.HostingEnvironment.MapPath("~" + baseDirectory);
                if (!System.IO.Directory.Exists(imagePath))
                    System.IO.Directory.CreateDirectory(imagePath);
                imagePath += fileName;

                if (System.IO.File.Exists(imagePath))
                    return ApplicationSettings.BaseUrl + baseDirectory + fileName;

                var photo = await graphClient.Groups[teamId].Photo.Content.Request().GetAsync();
                using (var fileStream = System.IO.File.Create(imagePath))
                {
                    photo.Seek(0, SeekOrigin.Begin);
                    photo.CopyTo(fileStream);
                }
                profilePhotoUrl = ApplicationSettings.BaseUrl + baseDirectory + fileName;
            }
            catch (Exception ex)
            {
                ErrorLogService.LogError(ex);
                profilePhotoUrl = ApplicationSettings.BaseUrl + "/Resources/Team.png";
            }
            return profilePhotoUrl;
        }


        public static async Task<string> POST(string url, string body)
        {
            HttpClient httpClient = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
            HttpResponseMessage response = await httpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception(responseBody);
            return responseBody;
        }

        public async Task<User> GetMe()
        {
            var graphClient = GetAuthenticatedClient();
            var me = await graphClient.Me.Request().GetAsync();
            return me;
        }

        public async Task<System.IO.Stream> GetProfilePhoto()
        {
            var graphClient = GetAuthenticatedClient();
            var me = await graphClient.Me.Photo.Content.Request().GetAsync();
            return me;
        }

        public async Task<User> GetManager()
        {
            var graphClient = GetAuthenticatedClient();
            User manager = await graphClient.Me.Manager.Request().GetAsync() as User;
            return manager;
        }

        private GraphServiceClient GetAuthenticatedClient()
        {
            GraphServiceClient graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    (requestMessage) =>
                    {
                        string accessToken = _token;

                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                        // Get event times in the current time zone.
                        requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + TimeZoneInfo.Local.Id + "\"");

                        return Task.CompletedTask;
                    }));
            return graphClient;
        }

        #region Graph HTTP request

        public async Task<string> CreateNewTeam(NewTeamDetails teamDetails)
        {
            var groupId = await CreateGroupAsyn(teamDetails);
            if (IsValidGuid(groupId))
            {
                await Common.ForEachAsync(teamDetails.UserADIds, 10,
                  async userid =>
                  {
                      var result = await AddTeamMemberAsync(groupId, userid);
                      if (!result)
                          Console.WriteLine($"Failed to add {userid} to {teamDetails.TeamName}. Check if user is already part of this team.");
                  }
                   );

                Console.WriteLine($"O365 Group is created for {teamDetails.TeamName}.");
                // Sometimes Team creation fails due to internal error. Added rety mechanism.
                var retryCount = 4;
                string teamId = null;
                do
                {
                    teamId = await CreateTeamAsyn(groupId); // getting response as 403: forbidden
                    if (IsValidGuid(teamId))
                    {
                        return teamId;
                    }
                    else
                    {
                        teamId = null;
                    }
                    retryCount--;
                    await Task.Delay(5000);
                } while (retryCount > 0);
            }
            return null;
        }

        bool IsValidGuid(string guid)
        {
            Guid teamGUID;
            return Guid.TryParse(guid, out teamGUID);
        }

        public async Task<string> CreateGroupAsyn(NewTeamDetails newTeamDetails)
        {
            string endpoint = ApplicationSettings.GraphApiEndpoint + "groups/";

            GroupInfo groupInfo = new GroupInfo()
            {
                description = "Team for " + newTeamDetails.TeamName,
                displayName = newTeamDetails.TeamName,
                groupTypes = new string[] { "Unified" },
                mailEnabled = true,
                mailNickname = newTeamDetails.TeamName.Replace(" ", "").Replace("-", "") + DateTime.Now.Second,
                securityEnabled = true,
                ownersodatabind = newTeamDetails.OwnerADIds.Select(userId => "https://graph.microsoft.com/v1.0/users/" + userId).ToArray()
            };

            return await PostRequest(endpoint, JsonConvert.SerializeObject(groupInfo));
        }


        public async Task<bool> AddTeamMemberAsync(string teamId, string userId)
        {
            string endpoint = ApplicationSettings.GraphApiEndpoint + $"groups/{teamId}/members/$ref";

            var userData = $"{{ \"@odata.id\": \"https://graph.microsoft.com/v1.0/users/{userId}\" }}";

            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                    request.Content = new StringContent(userData, Encoding.UTF8, "application/json");

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public async Task<string> CreateTeamAsyn(string groupId)
        {
            // This might need Retries.
            string endpoint = ApplicationSettings.GraphApiEndpoint + $"groups/{groupId}/team";

            TeamSettings teamInfo = new TeamSettings()
            {
                funSettings = new Funsettings() { allowGiphy = true, giphyContentRating = "strict" },
                messagingSettings = new Messagingsettings() { allowUserEditMessages = true, allowUserDeleteMessages = true },
                memberSettings = new Membersettings() { allowCreateUpdateChannels = true }
            };
            return await PutRequest(endpoint, JsonConvert.SerializeObject(teamInfo));
        }

        private async Task<string> PostRequest(string endpoint, string groupInfo)
        {
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
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

        private async Task<string> PutRequest(string endpoint, string groupInfo)
        {
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Put, endpoint))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
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

        public async Task<List<UserDetail>> FetchAllTenantMembersAsync(string nextUrl = null)
        {
            string endpoint = string.IsNullOrEmpty(nextUrl) ? ApplicationSettings.GraphApiEndpoint + $"users?$select=id,displayName,mail,userPrincipalName" : nextUrl;

            List<UserDetail> allMembers = new List<UserDetail>();

            using (var client = new HttpClient())
            {

                using (var request = new HttpRequestMessage(HttpMethod.Get, endpoint))
                {

                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<TenantUserList>(await response.Content.ReadAsStringAsync());
                            try
                            {
                                if (!string.IsNullOrEmpty(result.odatanextLink))
                                {
                                    var members = await FetchAllTenantMembersAsync(result.odatanextLink);
                                    allMembers.AddRange(members);
                                }
                                allMembers.AddRange(result.value);

                                return allMembers;
                            }
                            catch (Exception)
                            {
                                // Handle edge case.
                            }
                        }
                        return allMembers;
                    }
                }
            }
        }

        public async Task<List<UserDetail>> FetchAllGroupMembersAsync(string groupId, string nextUrl = null)
        {
            string endpoint = string.IsNullOrEmpty(nextUrl) ? ApplicationSettings.GraphApiEndpoint + $"groups/{groupId}/members?$select=id,displayName,mail,userPrincipalName" : nextUrl;

            List<UserDetail> allMembers = new List<UserDetail>();

            using (var client = new HttpClient())
            {

                using (var request = new HttpRequestMessage(HttpMethod.Get, endpoint))
                {

                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<TenantUserList>(await response.Content.ReadAsStringAsync());
                            try
                            {
                                if (!string.IsNullOrEmpty(result.odatanextLink))
                                {
                                    var members = await FetchAllTenantMembersAsync(result.odatanextLink);
                                    allMembers.AddRange(members);
                                }
                                allMembers.AddRange(result.value);

                                return allMembers;
                            }
                            catch (Exception)
                            {
                                // Handle edge case.
                            }
                        }
                        return allMembers;
                    }
                }
            }
        }

        #endregion
    }



    #region POCOs for team creation

    public class TokenResponse
    {
        public string access_token { get; set; }
    }

    public class NewTeamDetails
    {
        // public string OwnerEmailId { get; set; }
        public string TeamName { get; set; }
        public List<string> ChannelNames { get; set; } = new List<string>();
        public List<string> OwnerADIds { get; set; }
        public List<string> UserADIds { get; set; }

    }

    public class ResponseData
    {
        public string id { get; set; }
    }

    public class TenantUserList
    {
        [JsonProperty("@odata.context")]
        public string odatacontext { get; set; }

        [JsonProperty("@odata.nextLink")]
        public string odatanextLink { get; set; }
        public UserDetail[] value { get; set; }
    }

    public class UserDetail
    {
        public string id { get; set; }
        public string displayName { get; set; }
        public string mail { get; set; }
        public string userPrincipalName { get; set; }
    }

    public class GroupInfo
    {
        public string description { get; set; }
        public string displayName { get; set; }
        public string[] groupTypes { get; set; }
        public bool mailEnabled { get; set; }
        public string mailNickname { get; set; }
        public bool securityEnabled { get; set; }

        [JsonProperty("owners@odata.bind")]
        public string[] ownersodatabind { get; set; }

    }

    public class TeamSettings
    {
        public Membersettings memberSettings { get; set; }
        public Messagingsettings messagingSettings { get; set; }
        public Funsettings funSettings { get; set; }
    }

    public class Membersettings
    {
        public bool allowCreateUpdateChannels { get; set; }
    }

    public class Messagingsettings
    {
        public bool allowUserEditMessages { get; set; }
        public bool allowUserDeleteMessages { get; set; }
    }

    public class Funsettings
    {
        public bool allowGiphy { get; set; }
        public string giphyContentRating { get; set; }
    }
    #endregion

}