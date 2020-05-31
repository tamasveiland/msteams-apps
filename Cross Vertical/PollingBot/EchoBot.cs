using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using CrossVertical.PollingBot.Helper;
using CrossVertical.PollingBot.Models;
using CrossVertical.PollingBot.Repository;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

namespace CrossVertical.PollingBot
{
    [Serializable]
    public class EchoBot : IDialog<object>
    {
        private static readonly string ConnectionName = ConfigurationManager.AppSettings["ConnectionName"];
        private const string LastAction = "LastAction";
        private static readonly string LastCommand = string.Empty;
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var activity = await argument as Activity;
            bool IsAdmin = false;
            var message = string.Empty;
            var userAdmin = await DocumentDBRepository.GetItemsAsync<Admin>(a => a.Type == "Admin");
            var userAdminData = new Admin();
            var useremailId = await GetUserEmailId(activity);
            if (userAdmin != null && userAdmin.Count() > 0)
            {
                userAdminData = userAdmin.FirstOrDefault();
                if (userAdminData.EmailId.ToLower() == useremailId.ToLower())
                    IsAdmin = true;
            }
            else
            {
                IsAdmin = false;
            }
            
            if (activity.Text == null)
                activity.Text = string.Empty;
                    
                
                if(activity.Text!=null)
                    message = Microsoft.Bot.Connector.Teams.ActivityExtensions.GetTextWithoutMentions(activity).ToLowerInvariant().Trim();

                if (message.Equals("help") || message.Equals("hi") || message.Equals("hello"))
            {
                if (IsAdmin == true)
                {
                    await SendHelpMessage(context, activity);
                }
                else
                {
                    await SendWelcomeMesssage(context, activity);
                }
                await AddDatabase(activity);
            }
            else if(activity.Value!=null && activity.Type!=message)
                {
                    if (message == "create survey")
                    {
                        context.UserData.SetValue(LastAction, message);
                        await CreateSurvey(context, activity);
                    }
                    else if (message == "download survey")
                    {
                        context.UserData.SetValue(LastAction, message);
                        await CreateExcel(context, activity);
    
                    }
                    else if(message=="send reminder")
                    {
                        await SendReminderMessage(context, activity);
                    }
                    else
                    {
                        await HandleActions(context, activity);
                    }
                }
                else if(message=="set admin")
                {
                    await SendSetManagerCard(context, activity, userAdminData, useremailId.ToString());

                }
                else
                {
                    // Check for file upload.
                    if (activity.Attachments != null && activity.Attachments.Any())
                    {
                       try
                        {
                            var attachment = activity.Attachments.First();
                            await HandleExcelAttachement(context, activity, attachment);
                        }
                        catch (Exception ex)
                        {
                            await context.PostAsync(ex.ToString());
                        }
                    }
                }
            
        }

        private static async Task AddDatabase(Activity activity)
        {
            UserDetails user = new UserDetails();
            user.EmaildId = await GetUserEmailId(activity);
            user.UserName = await GetUserName(activity);
            user.UserId = activity.From.Id;
            if (user.UserName != null)
            {
                user.UserName = user.UserName.Split(' ').FirstOrDefault();
            }
            user.Type = Helper.Constants.NewUser;
            var existinguserRecord = await DocumentDBRepository.GetItemsAsync<UserDetails>(u => u.EmaildId == user.EmaildId && u.Type == Helper.Constants.NewUser);
            if (existinguserRecord.Count() == 0)
            {
                var NewUserRecord = await DocumentDBRepository.CreateItemAsync(user);
            }
        }

        private static async Task SetEmaployeeManager(IDialogContext context, Activity activity, InputDetails input)
        {
            Admin adminData = new Admin();
            adminData.EmailId = input.txtAdmin;
            adminData.Type = "Admin";
            await AdminEmailId(context, adminData);

            var reply = activity.CreateReply();
            reply.Text = "Your Admin is set successfully. Admin Email Id: " + input.txtAdmin;
            await context.PostAsync(reply);
        }
        private static async Task AdminEmailId(IDialogContext context, Admin userAdmin)
        {
            var existingAdmin = await DocumentDBRepository.GetItemsAsync<Admin>(l => l.Type.Contains(Helper.Constants.SetAdmin));
            if (existingAdmin.Count() > 0)
            {
                var Admin = existingAdmin.FirstOrDefault();
                Admin.EmailId = userAdmin.EmailId;
                var updateAdmin = await DocumentDBRepository.UpdateItemAsync(Admin.Id, Admin);
            }
            else
            {
                userAdmin.Type = "Admin";
                await DocumentDBRepository.CreateItemAsync<Admin>(userAdmin);
            }
        }

        private static async Task SendSetManagerCard(IDialogContext context, Activity activity, Admin userAdmin, string message)
        {
            //Ask for manager details.
            var card = EchoBot.SetManagerCard(); // WelcomeLeaveCard(employee.Name.Split(' ').First());
            var msg = context.MakeMessage();
            msg.Text = "Please set your admin for the survey";
            msg.Attachments.Add(card);
            await context.PostAsync(msg);
        }

        public static List<string> ExtractEmails(string str)
        {
            string RegexPattern = @"\b[A-Z0-9._-]+@[A-Z0-9][A-Z0-9.-]{0,61}[A-Z0-9]\.[A-Z.]{2,6}\b";

            // Find matches
            System.Text.RegularExpressions.MatchCollection matches
                = System.Text.RegularExpressions.Regex.Matches(str, RegexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            List<string> MatchList = new List<string>(matches.Count);

            // add each match
            int c = 0;
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                MatchList.Add(match.ToString());
                c++;
            }

            return MatchList;
        }


        public static Attachment SetManagerCard()
        {
            var Card = new AdaptiveCard()
            {
                Body = new List<AdaptiveElement>()
                          {
                              new AdaptiveTextBlock(){Text="Enter Admin Email Id:"},
                              new AdaptiveTextInput(){Id="txtAdmin", IsMultiline=false, Style = AdaptiveTextInputStyle.Email, IsRequired=true, Placeholder="Admin Email Id"}
                          },
                Actions = new List<AdaptiveAction>()
                          {
                              new AdaptiveSubmitAction()
                              {
                                  Title="Set Admin",
                                  DataJson= @"{'Type':'" + Constants.SetAdmin+"'}"
                              }
                          }
            };

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = Card
            };
        }

        private async Task HandleActions(IDialogContext context, Activity activity)
        {
            string type = string.Empty;
            InputDetails obj = JsonConvert.DeserializeObject<InputDetails>(activity.Value.ToString());
            type = obj.Type;

            var reply = activity.CreateReply();

            switch (type)
            {
                case Helper.Constants.Submit:
                    await GetFeedback(context, activity, obj);
                    break;

                case Helper.Constants.SubmitSurvey:
                    await GetSubmitSurvey(context,activity,obj);
                    break;

                case Helper.Constants.PublishSurvey:
                    await PublishSurvery(context, activity, obj);
                    //var attachment = EchoBot.PoolCreationCard();
                    break;
                case Helper.Constants.SetAdmin:
                    await SetEmaployeeManager(context, activity, obj);
                    break;
                default:
                    reply = activity.CreateReply("It will redirect to the tab");
                    await context.PostAsync(reply);
                    break;
            }
        }

        public static async Task<string> SendNotification(IDialogContext context, string userOrChannelId, string messageText, Microsoft.Bot.Connector.Attachment attachment, string updateMessageId)
        {
            var userId = userOrChannelId.Trim();
            var botId = context.Activity.Recipient.Id;
            var botName = context.Activity.Recipient.Name;
            var channelData = context.Activity.GetChannelData<TeamsChannelData>();
            var connectorClient = new ConnectorClient(new Uri(context.Activity.ServiceUrl));
            var parameters = new ConversationParameters
            {
                Bot = new ChannelAccount(botId, botName),
                Members = new ChannelAccount[] { new ChannelAccount(userId) },
                ChannelData = new TeamsChannelData
                {
                    Tenant = channelData.Tenant,
                    Notification = new NotificationInfo() { Alert = true }
                },
                IsGroup = false
            };

            try
            {
                var conversationResource = await connectorClient.Conversations.CreateConversationAsync(parameters);
                var replyMessage = Activity.CreateMessageActivity();
                replyMessage.From = new ChannelAccount(botId, botName);
                replyMessage.Conversation = new ConversationAccount(id: conversationResource.Id.ToString());
                replyMessage.ChannelData = new TeamsChannelData() { Notification = new NotificationInfo(true) };
                replyMessage.Text = messageText;
                replyMessage.TextFormat = TextFormatTypes.Markdown;
                if (attachment != null)
                {
                    replyMessage.Attachments.Add(attachment); // .Add(attachment);//  EchoBot.ManagerViewCard(employee, leaveDetails));
                    //replyMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                }
                if (string.IsNullOrEmpty(updateMessageId))
                {
                    var resourceResponse = await connectorClient.Conversations.SendToConversationAsync(conversationResource.Id, (Activity)replyMessage);
                    return null;
                    //return resourceResponse.Id;
                }
                else
                {
                    await connectorClient.Conversations.UpdateActivityAsync(conversationResource.Id, updateMessageId, (Activity)replyMessage);
                    return updateMessageId; // Just return the same Id.
                }
            }
            catch (Exception ex)
            {
                // Handle the error.
                var msg = context.MakeMessage();
                msg.Text = ex.Message;
                await context.PostAsync(msg);
                return null;
            }
        }
        public static async Task PublishSurvery(IDialogContext context, Activity activity, InputDetails input)
        {
            var questionbank = await DocumentDBRepository.GetItemAsync<QuestionBank>(input.QuestionBankId);
            //var questionbank = questionb
            string userUniqueId = null;
            string userEmailId = null;
            var attachment = new Attachment();
            if (questionbank != null)
            {
                if (questionbank.Active == true)
                {
                    Attachment attachments = new Attachment();
                    var tempcount = 1;
                   
                        foreach (var questions in questionbank.Questions)
                        {
                            if (tempcount == 1)
                            {
                                var question = new Question();
                                question.Id = questions.Id;
                                question.Title = questions.Title;
                                question.Options = questions.Options;
                                attachment=PollCreationCard(questionbank.Id, question, false, questionbank.Questions.Count(), tempcount);
                                tempcount = tempcount + 1;
                                break;
                            }
                        }
                    

                    foreach (var emailid in questionbank.EmailIds)
                    {
                        var userData = await DocumentDBRepository.GetItemsAsync<UserDetails>(u => u.EmaildId.ToLower() == emailid.ToLower());
                        var usersData = userData.FirstOrDefault();
                        if (userData.Count()>0)
                        {
                            userUniqueId = usersData.UserId;
                            userEmailId = usersData.EmaildId;
                            var MessageText = "**Hey " + usersData.UserName + "!!  Welcome to the survey application. Please fill the survey details:";
                            await SendNotification(context, userUniqueId, MessageText, attachment, null);
                        }
                    }
                }

            }
        }

        private static async Task GetSubmitSurvey(IDialogContext context, Activity activity, InputDetails input)
        {
            FeedbackData objFeedBack = null;

            //activity.ReplyToId
            if (!context.ConversationData.ContainsKey(input.QuestionBankId))
            {
                objFeedBack = new FeedbackData();
                objFeedBack.QuestionBankId = input.QuestionBankId;
                objFeedBack.Type = Helper.Constants.FeedBack;
                objFeedBack.UserEmailId = await GetUserEmailId(activity);
                objFeedBack.Active = true;
            }
            else
            {
                objFeedBack = context.ConversationData.GetValue<FeedbackData>(input.QuestionBankId);
            }

            objFeedBack.Feedback.Add(input.QuestionId, input.Options);
            context.ConversationData.SetValue(objFeedBack.QuestionBankId, objFeedBack);

            if (context.ConversationData.ContainsKey(input.QuestionBankId))
            { 
                var FeedBackDataInsert = context.ConversationData.GetValue<FeedbackData>(input.QuestionBankId);
                var FeedbackInsert = await DocumentDBRepository.CreateItemAsync(FeedBackDataInsert);
                await context.PostAsync("Thanks for your time. Your feedback is completed.");
            }
            else
            {
                await context.PostAsync("Please fill all the options before submitting survey");
            }
            
        }

        private static async Task<string> GetUserEmailId(Activity activity)
        {
            // Fetch the members in the current conversation
            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
            var members = await connector.Conversations.GetConversationMembersAsync(activity.Conversation.Id);
            return members.Where(m => m.Id == activity.From.Id).First().AsTeamsChannelAccount().UserPrincipalName;
        }

        private static async Task<string> GetUserName(Activity activity)
        {
            // Fetch the members in the current conversation
            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
            var members = await connector.Conversations.GetConversationMembersAsync(activity.Conversation.Id);
            return members.Where(m => m.Id == activity.From.Id).First().AsTeamsChannelAccount().Name;
        }
        private static async Task GetFeedback(IDialogContext context, Activity activity, InputDetails input)
        {
            FeedbackData objFeedBack = null;
           
            //activity.ReplyToId
            if (!context.ConversationData.ContainsKey(input.QuestionBankId))
            {
                objFeedBack = new FeedbackData();
                objFeedBack.QuestionBankId = input.QuestionBankId;
                objFeedBack.Type = Helper.Constants.FeedBack;
                objFeedBack.UserEmailId = await GetUserEmailId(activity);
                objFeedBack.Active = true;
            }
            else
            {
                objFeedBack = context.ConversationData.GetValue<FeedbackData>(input.QuestionBankId);
            }

            objFeedBack.Feedback.Add(input.QuestionId, input.Options);
            context.ConversationData.SetValue(objFeedBack.QuestionBankId, objFeedBack);
            var questionbank = await DocumentDBRepository.GetItemAsync<QuestionBank>(input.QuestionBankId);
            string userUniqueId = null;
            string userEmailId = null;
            if (questionbank != null)
            {
                if (questionbank.Active == true)
                {
                    var attachment= new Attachment();
                    var tempcount = 1;
                    for(int i=0;i<questionbank.Questions.Count();i++)
                    {
                        if(input.QuestionId==questionbank.Questions[i].Title)
                        {
                            var question = new Question();
                            question.Id = questionbank.Questions[i+1].Id;
                            question.Title = questionbank.Questions[i+1].Title;
                            question.Options = questionbank.Questions[i+1].Options;
                            tempcount = tempcount + i+1;
                            attachment = (PollCreationCard(questionbank.Id, question, false, questionbank.Questions.Count(),tempcount ));

                        }
                    }
                   
                    var currentEmailid = await GetUserEmailId(activity);
                        var userData = await DocumentDBRepository.GetItemsAsync<UserDetails>(u => u.EmaildId == currentEmailid);
                        var usersData = userData.FirstOrDefault();
                        if (userData.Count() > 0)
                        {
                            userUniqueId = usersData.UserId;
                            userEmailId = usersData.EmaildId;
                            await SendNotification(context, userUniqueId, null, attachment, activity.ReplyToId);
                        }
                    
                }

            }
            
        }
        private static ThumbnailCard GetThumbnailForTeamsAction()
        {
            return new ThumbnailCard
            {
                Text = @"Please go ahead and upload the spreadsheet with survey details in the following format:  
                        <ol>
                        <li><strong>Questions</strong>: String eg: <pre>How are you?</pre></li>
                        <li><strong>Options</strong> : Comma separated option names eg: <pre>option 1, option 2</pre></li>
                        <li><strong>Members</strong>  : Comma separated user emails eg: <pre>user1@org.com, user2@org.com</pre></li></ol>
                         </br> <strong>Note: Please keep first row header as described above. You can provide details for multiple questions row by row. •	Members input box is not a mandatory field </strong>",
                Buttons = new List<CardAction>(),
            };
        }

        private async Task CreateSurvey(IDialogContext context, Activity activity)
        {

            Activity reply = activity.CreateReply();

            ThumbnailCard card = GetThumbnailForTeamsAction();
            card.Title = "Create a new survey";
            card.Subtitle = "Automate survey creation by sharing question and options";

            reply.TextFormat = TextFormatTypes.Xml;
            reply.Attachments.Add(card.ToAttachment());
            await context.PostAsync(reply);

        }

        private async Task SendReminderMessage(IDialogContext context, Activity activity)
        {
            var objQuestionBank = await DocumentDBRepository.GetItemsAsync<QuestionBank>(l => l.Type.Contains(Helper.Constants.QuestionBank) && l.Active == true);
            var feedbackData = await DocumentDBRepository.GetItemsAsync<FeedbackData>(l => l.Type.Contains(Helper.Constants.FeedBack) && l.Active == true);
            string userUniqueId = null;
            string userEmailId = null;
            if (objQuestionBank.Count()>0)
            {
                var questionbankData = objQuestionBank.FirstOrDefault();
                //List<Attachment> attachments = new List<Attachment>();
                Attachment attachments = new Attachment();
                var tempcount = 1;
                
                foreach (var questions in questionbankData.Questions)
                {
                    if (tempcount == 1)
                    {
                        var question = new Question();
                        question.Id = questions.Id;
                        question.Title = questions.Title;
                        question.Options = questions.Options;
                        attachments = (PollCreationCard(questionbankData.Id, question, false, questionbankData.Questions.Count(), tempcount));
                        tempcount = tempcount + 1;
                        break;
                    }
                }
                int reminderCount = 0;
                foreach (var emailid in questionbankData.EmailIds)
                {
                    if (feedbackData.Count() > 0)
                    {
                        //var feedbackEmailId = feedbackData.FirstOrDefault();
                        foreach(var feedbackemaials in feedbackData)
                        {
                            if(feedbackemaials.UserEmailId!=emailid)
                            {
                                
                                var userData = await DocumentDBRepository.GetItemsAsync<UserDetails>(u => u.EmaildId == emailid);
                                var usersData = userData.FirstOrDefault();
                                if (userData.Count()>0)
                                {
                                    userUniqueId = usersData.UserId;
                                    userEmailId = usersData.EmaildId;
                                    var MessageText = "**Hey " + usersData.UserName + "!!  ATTENTION. Please fill the survey details:**";                                   
                                    await SendNotification(context, userUniqueId, MessageText, attachments, null);
                                    reminderCount = reminderCount + 1;
                                }
                            }
                        }
                    }
                    else
                    {
                        var userData = await DocumentDBRepository.GetItemsAsync<UserDetails>(u => u.EmaildId == emailid);
                        var usersData = userData.FirstOrDefault();
                        
                        if (userData.Count()>0)
                        {
                            userUniqueId = usersData.UserId;
                            userEmailId = usersData.EmaildId;
                            var MessageText = "**Hey " + usersData.UserName + "!!  ATTENTION. Please fill the survey details:**";
                            await SendNotification(context, userUniqueId, MessageText, attachments, null);
                            reminderCount = reminderCount + 1;
                        }
                    }
                }
                if (reminderCount > 0)
                {
                    await context.PostAsync("Reminders sent to " + reminderCount + " users");
                }
                else
                {
                    await context.PostAsync("No reminders sent. All users filled the survey");
                }

            }
            else
            {
                await context.PostAsync("No active questions to fill the survey");
            }
            

        }

        private async Task CreateExcel(IDialogContext context, Activity activity)
        {
            try
            {
                var objFeedBackData = await DocumentDBRepository.GetItemsAsync<FeedbackData>(l => l.Type.Contains("FeedBack") && l.Active == true);
                if (objFeedBackData.Count() > 0 && objFeedBackData != null)
                {
                    try
                    {

                        DataTable feedBack = new DataTable("FeedBack");
                        feedBack.Columns.Add("Employee Email Id");
                        feedBack.Columns.Add("Questions");
                        feedBack.Columns.Add("Options");
                        foreach (var objfeedBack in objFeedBackData)
                        {
                            foreach (var objquestion in objfeedBack.Feedback)
                            {
                                feedBack.Rows.Add(objfeedBack.UserEmailId, objquestion.Key, objquestion.Value);
                            }
                        }
                        DataSet ds = new DataSet("FeedBacktable");
                        ds.Tables.Add(feedBack);
                        string ExcelPath = System.Web.Hosting.HostingEnvironment.MapPath("~/FeedBack/");
                        var fileName = "FeedBack.csv";
                        if (!System.IO.Directory.Exists(ExcelPath))
                            System.IO.Directory.CreateDirectory(ExcelPath);
                        ExcelPath += fileName;
                        var feedbackdata = ConfigurationManager.AppSettings["BaseUri"] + "/FeedBack/" + fileName;
                        CreateCSVFile(feedBack, ExcelPath);
                        String AnchorLink;
                        AnchorLink = "<" + "a href=" + feedbackdata + " target=_blank>" + "Download" + "</a>";
                        Activity reply = activity.CreateReply("Download completed. Click here" + AnchorLink+"to collect the results");
                        await context.PostAsync(reply);
                    }
                    catch (Exception e)
                    {
                        await context.PostAsync(e.Message);
                    }
                }
                else
                {
                    await context.PostAsync("No data found to download");
                }
            }
            catch(Exception e)
            {
                await context.PostAsync(e.Message.ToString());
            }
            

        }
        public static async Task SendWelcomeMesssage(IDialogContext context, Activity activity)
        {
            Activity reply = activity.CreateReply();
            ThumbnailCard card = GetWelcomeMessage();

            reply.TextFormat = TextFormatTypes.Xml;
            reply.Attachments.Add(card.ToAttachment());
            await context.PostAsync(reply);
        }


        public static async Task SendHelpMessage(IDialogContext context, Activity activity)
        {
            Activity reply = activity.CreateReply();
            ThumbnailCard card = GetHelpMessage();

            reply.TextFormat = TextFormatTypes.Xml;
            reply.Attachments.Add(card.ToAttachment());
            await context.PostAsync(reply);
        }

        private static async Task HandleExcelAttachement(IDialogContext context, Activity activity, Attachment attachment)
        {
            if (attachment.ContentType == FileDownloadInfo.ContentType)
            {
                FileDownloadInfo downloadInfo = (attachment.Content as JObject).ToObject<FileDownloadInfo>();
                var filePath = System.Web.Hosting.HostingEnvironment.MapPath("~/Files/");
                if (!Directory.Exists(filePath))
                    Directory.CreateDirectory(filePath);

                filePath += attachment.Name + DateTime.Now.Millisecond; // just to avoid name collision with other users. 
                if (downloadInfo != null)
                {
                    using (WebClient myWebClient = new WebClient())
                    {
                        // Download the Web resource and save it into the current filesystem folder.
                        myWebClient.DownloadFile(downloadInfo.DownloadUrl, filePath);

                    }
                    if (File.Exists(filePath))
                    {
                        var InsertTeamid = ExcelHelper.GetAddSurveyDetailsAsync(filePath);

                        if (InsertTeamid == null)
                        {
                            await context.PostAsync($"Attachment received but unfortunately we are not able to read your file. Please make sure that all the columns are correct.");
                        }
                        else
                        {
                            string lastAction;

                            if (context.UserData.TryGetValue(LastAction, out lastAction))
                            {

                                await context.PostAsync($"Attachment received. Working on getting your survey ready.");
                                var questioBankData = await DocumentDBRepository.GetItemsAsync<QuestionBank>(l => l.Id == InsertTeamid.Result.ToString());
                                Activity reply = activity.CreateReply();
                                bool IsAdmin;
                                IsAdmin = true;

                                reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                                int tempcount = 1;

                                if (questioBankData != null)
                                {
                                    foreach (var questionbank in questioBankData)
                                    {
                                        foreach (var questions in questionbank.Questions)
                                        {
                                            var question = new Question();
                                            question.Id = questions.Id;
                                            question.Title = questions.Title;
                                            question.Options = questions.Options;
                                            reply.Attachments.Add(PollCreationCard(questionbank.Id, question, IsAdmin, questionbank.Questions.Count(), tempcount));
                                            tempcount = tempcount + 1;
                                        }
                                    }
                                }
                                await context.PostAsync(reply);


                            }
                            else
                            {
                                await context.PostAsync($"Not able to process your file. Please restart the flow.");
                                await SendHelpMessage(context, activity);
                            }

                        }

                        File.Delete(filePath);
                    }
                }
            }
        }

        internal static ThumbnailCard GetWelcomeMessage()
        {
            ThumbnailCard card = new ThumbnailCard
            {
                Title = "Welcome to Survey Application",
                Subtitle = "Your Survey Assistant ",
            };

            return card;
        }

        internal static ThumbnailCard GetHelpMessage()
        {
            ThumbnailCard card = new ThumbnailCard
            {
                Title = "Welcome to Polling Bot",
                Subtitle = "Your aide in creating & collecting feedback from team members",
                Text = @"Use the bot to: 
                        <ol><li>Create a new survey by uploading a spreadsheet with member details</li><li>Download survey feedback from Team Members</li></ol>",
                Buttons = new List<CardAction>(),
            };

            card.Buttons.Add(new CardAction
            {
                Title = "Create Survey",
                DisplayText = "Create Survey",
                Type = ActionTypes.MessageBack,
                Text = "create survey",
                Value = "create survey"

            });
            card.Buttons.Add(new CardAction
            {
                Title = "Download Survey",
                DisplayText = "Download Survey",
                Type = ActionTypes.MessageBack,
                Text = "download survey",
                Value = "download survey"

            });
            card.Buttons.Add(new CardAction
            {
                Title = "Send Reminder",
                DisplayText = "Send Reminder",
                Type = ActionTypes.MessageBack,
                Text = "send reminder",
                Value = "send reminder"

            });

            return card;
        }
       

        public void CreateCSVFile(DataTable dt, string strFilePath)
        {
            StreamWriter sw = new StreamWriter(strFilePath, false);
            int iColCount = dt.Columns.Count;
            for (int i = 0; i < iColCount; i++)
            {
                sw.Write(dt.Columns[i]);
                if (i < iColCount - 1)
                {
                    sw.Write(",");
                }
            }
            sw.Write(sw.NewLine);
            // Now write all the rows.
            foreach (DataRow dr in dt.Rows)
            {
                for (int i = 0; i < iColCount; i++)
                {
                    if (!Convert.IsDBNull(dr[i]))
                    {
                        sw.Write(dr[i].ToString());
                    }
                    if (i < iColCount - 1)
                    {
                        sw.Write(",");
                    }
                }
                sw.Write(sw.NewLine);
            }
            sw.Close();
        }

        public static Attachment PollCreationCard(string QuestionBankId, Question surveyDetails, bool IsAdmin, int TotalCount, int tempCount)
        {
            var questions = new AdaptiveTextBlock();
            var options = new List<AdaptiveChoice>();
            if (surveyDetails != null)
            {
                questions = new AdaptiveTextBlock() { Text = tempCount + "."+surveyDetails.Title.ToString(), Color = AdaptiveTextColor.Default, Size = AdaptiveTextSize.Medium, HorizontalAlignment=AdaptiveHorizontalAlignment.Left };
                for (int i = 0; i < surveyDetails.Options.Count; i++)
                {
                    options.Add(new AdaptiveChoice() { Title = surveyDetails.Options[i], Value = surveyDetails.Options[i], });

                }
            }
            var PoolCreationCard = new AdaptiveCard()
            {
                Body = new List<AdaptiveElement>()
                {
                    questions,
                    new AdaptiveChoiceSetInput(){Id="Options",  Choices=new List<AdaptiveChoice>(options), IsMultiSelect=false,Style=AdaptiveChoiceInputStyle.Expanded,
                                             Value =surveyDetails.Options[0] }
                }
            };
            if (IsAdmin && (tempCount == TotalCount))
            {
                PoolCreationCard.Actions.Insert(0,
                    new AdaptiveSubmitAction()
                    {
                        Title = "Publish Survey",
                        DataJson = @"{'Type':'" + Helper.Constants.PublishSurvey + "', 'QuestionBankId':'" + QuestionBankId + "'}"
                    });
            }
            else if (IsAdmin == false && (tempCount == TotalCount))
            {
                
                PoolCreationCard.Actions.Insert(0,
                    new AdaptiveSubmitAction()
                    {
                        Title = "Submit Survey",
                        DataJson = @"{'Type':'" + Helper.Constants.SubmitSurvey + "', 'QuestionBankId':'" + QuestionBankId + "', 'QuestionId':'" + surveyDetails.Title + "' }"
                    });

            }
            else if (IsAdmin == false)
            {
                PoolCreationCard.Actions.Insert(0,
                    new AdaptiveSubmitAction()
                    {
                        Title = "Next",
                        DataJson = @"{'Type':'" + Helper.Constants.Submit + "' , 'QuestionBankId':'" + QuestionBankId + "', 'QuestionId':'" + surveyDetails.Title + "' }"
                    });
            }

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = PoolCreationCard
            };
        }

    }


}
