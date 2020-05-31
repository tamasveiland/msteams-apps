using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Airline.PassengerInfo.Web.Helper;
using Airline.PassengerInfo.Web.Model;
using Airline.PassengerInfo.Web.Repository;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;

namespace Airline.PassengerInfo.Web
{
    public class MessageExtension
    {
        public async static Task<ComposeExtensionResponse> HandleMessageExtensionQuery(Activity activity)
        {
            var query = activity.GetComposeExtensionQueryData();
            if (query == null || query.CommandId != "search")
            {
                // We only process the 'getRandomText' queries with this message extension
                return null;
            }

            var title = "";
            var titleParam = query.Parameters?.FirstOrDefault(p => p.Name == "searchText");
            if (titleParam != null)
            {
                title = titleParam.Value.ToString().ToLower();
            }

            var attachments = new List<ComposeExtensionAttachment>();

            var passengers = await DocumentDBRepository<Passenger>.GetItemsAsync(d => d != null && (d.Name.ToLower().Contains(title) || d.Seat.ToLower().Contains(title)));


            foreach (var passenger in passengers)
            {
                var card = O365CardHelper.GetO365ConnectorCard(passenger);
                var preview = O365CardHelper.GetPreviewCard(passenger);

                attachments.Add(card
                .ToAttachment()
                    .ToComposeExtensionAttachment(preview.ToAttachment()));
            }

            var response = new ComposeExtensionResponse(new ComposeExtensionResult("list", "result"));
            response.ComposeExtension.Attachments = attachments.ToList();

            return response;
        }
    }
}
