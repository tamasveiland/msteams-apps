using Autofac;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Airline.BaggageInfoBot.Web.Repository;
using System.Configuration;
using System.Reflection;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using Airline.BaggageInfoBot.Web.Model;
using Microsoft.Bot.Builder.Azure;

namespace Airline.BaggageInfoBot.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            DocumentDBRepository<Baggage>.Initialize(); // Initialize DB
            Conversation.UpdateContainer(
               builder =>
               {
                   builder.RegisterModule(new AzureModule(Assembly.GetExecutingAssembly()));

                   // Using Azure Table Storage
                   var store = new TableBotDataStore(ConfigurationManager.AppSettings["AzureWebJobsStorage"]); // requires Microsoft.BotBuilder.Azure Nuget package 

                   // To use CosmosDb or InMemory storage instead of the default table storage, uncomment the corresponding line below
                   // var store = new DocumentDbBotDataStore("cosmos db uri", "cosmos db key"); // requires Microsoft.BotBuilder.Azure Nuget package 
                   // var store = new InMemoryDataStore(); // volatile in-memory store

                   builder.Register(c => store)
                      .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                      .AsSelf()
                      .SingleInstance();

               });
        }
    }
}
