using Microsoft.Graph;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ProfessionalServices.LeaveBot.Helpers
{
    public class SimpleGraphClient
    {
        private readonly string _token;

        public SimpleGraphClient(string token)
        {
            _token = token;
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
                    async (requestMessage) =>
                    {
                        string accessToken = _token;

                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                        // Get event times in the current time zone.
                        requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + TimeZoneInfo.Local.Id + "\"");
                    }));
            return graphClient;
        }
    }
}