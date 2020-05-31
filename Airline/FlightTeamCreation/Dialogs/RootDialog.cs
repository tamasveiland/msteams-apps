using Airline.FlightTeamCreation.Web.Controllers;
using Airline.FlightTeamCreation.Web.Repository;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace Airline.FlightTeamCreation.Web
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        protected int count = 1;

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        string GraphRootUri = ConfigurationManager.AppSettings["GraphRootUri"];

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            var userInfo = UserInfoRepository.GetUserInfo(message.From.Id);
            
            if (userInfo == null || userInfo.ExpiryTime < DateTime.Now)
            {
                var reply = context.MakeMessage();

                SigninCard plCard = GetSignInCard();

                reply.Attachments.Add(plCard.ToAttachment());

                await context.PostAsync(reply);
                context.Wait(MessageReceivedAsync);
            }
            else
            {
                var msgText = message.Text;
                    var connector = new ConnectorClient(new Uri(context.Activity.ServiceUrl));

                    var email = string.Empty;
                    var member = connector.Conversations.GetConversationMembersAsync(message.Conversation.Id).Result.AsTeamsChannelAccounts().FirstOrDefault();
                    if (member != null)
                        email = member.Email;


                    var reply = context.MakeMessage();
                    var card = GetFilter(email);
                    reply.Attachments.Add(card);
                    await context.PostAsync(reply);
                    context.Wait(MessageReceivedAsync);

            }
        }

        public static SigninCard GetSignInCard()
        {
            string configUrl = ConfigurationManager.AppSettings["BaseUri"].ToString() + "/composeExtensionSettings.html";
            CardAction configExp = new CardAction(ActionTypes.Signin, "Sign In", null, configUrl);
            List<CardAction> lstCardAction = new List<CardAction>();
            lstCardAction.Add(configExp);

            SigninCard plCard = new SigninCard(text: "Please sign in to AAD acount. This app needs admin consent.", buttons: lstCardAction);
            return plCard;
        }

        public static Attachment GetFilter(string email)
        {


            var section = new O365ConnectorCardSection("Please select the option below to create Team");

            // "C:\Users\v-washai\Downloads\Picture1.png"
            // var heroImage = new O365ConnectorCardSection(null, null, null, null, null, "");

            var inputs = new List<O365ConnectorCardMultichoiceInputChoice>();

            foreach (var member in MessagesController.GetMemberList(email))
            {
                inputs.Add(new O365ConnectorCardMultichoiceInputChoice(member.Split('@').First(), member));
            }

            var memberSelection = new O365ConnectorCardMultichoiceInput(
                        O365ConnectorCardMultichoiceInput.Type,
                        "members",
                        true,
                        "Select Team Members",
                        null,
                        inputs
                        ,
                        "compact"
                        , true);

            var createCustomTeam = new O365ConnectorCardActionCard(
                O365ConnectorCardActionCard.Type,
                "Create Custom Team",
                "createCustomTeam",
                 new List<O365ConnectorCardInputBase>
                {
                      new O365ConnectorCardTextInput(
                        O365ConnectorCardTextInput.Type,
                        "teamName",
                        true,
                        "Enter Team Name",
                        null,
                        false,
                        null),
                      memberSelection
                 },
                new List<O365ConnectorCardActionBase>
                {
                   new O365ConnectorCardHttpPOST(
                        O365ConnectorCardHttpPOST.Type,
                        "Create Team",
                        "Custom",
                        @"{""TeamName"":""{{teamName.value}}"", ""Members"":""{{members.value}}""}")
                });

            var createTeamForFlight = new O365ConnectorCardActionCard(
                O365ConnectorCardActionCard.Type,
                "Create Team for Upcoming Flight",
                "flightWiseTeam",
                 new List<O365ConnectorCardInputBase>
                {

                    new  O365ConnectorCardMultichoiceInput(
                        O365ConnectorCardMultichoiceInput.Type,
                        "flight",
                        true,
                        "Select flight",
                        null,
                        new List<O365ConnectorCardMultichoiceInputChoice>
                        {
                            new O365ConnectorCardMultichoiceInputChoice("783", "783"),
                            new O365ConnectorCardMultichoiceInputChoice("784", "784"),
                            new O365ConnectorCardMultichoiceInputChoice("785", "785")
                        },
                        "compact"
                        ,false)
                 },
                new List<O365ConnectorCardActionBase>
                {
                   new O365ConnectorCardHttpPOST(
                        O365ConnectorCardHttpPOST.Type,
                        "Create Team",
                        "Flight",
                        @"{""Value"":""{{flight.value}}""}")
                });

            
            O365ConnectorCard card = new O365ConnectorCard()
            {
                ThemeColor = "#E67A9E",
                Title = "Welcome to FlightTeamCreationBot.",
                Summary = "",
                Sections = new List<O365ConnectorCardSection> { section },
                PotentialAction = new List<O365ConnectorCardActionBase>
                {
                    createTeamForFlight,
                    createCustomTeam
                }
            };
            return card.ToAttachment();
        }

        

    }
}