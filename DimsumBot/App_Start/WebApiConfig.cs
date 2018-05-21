using Autofac;
using Autofac.Integration.WebApi;
using CommonServiceLocator;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Http;
using Autofac.Extras.CommonServiceLocator;
using DimsumBot.Dispatch;
using Microsoft.Bot.Builder.Luis;

namespace DimsumBot
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Json settings
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Newtonsoft.Json.Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };

            //Associate bot with DB on Azure
            SetupBotDataStore();

            // Web API configuration and services
            var container = RegisterDependencies(new ContainerBuilder());

            var csl = new AutofacServiceLocator(container);
            ServiceLocator.SetLocatorProvider(() => csl);

            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }

        private static void SetupBotDataStore()
        {
            var uri = new Uri(ConfigurationManager.AppSettings["DocumentDbUrl"]);
            var key = ConfigurationManager.AppSettings["DocumentDbKey"];
            var store = new DocumentDbBotDataStore(uri, key);

            Conversation.UpdateContainer(
                builder =>
                {
                    builder.Register(c => store)
                        .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                        .AsSelf()
                        .SingleInstance();

                    builder.Register(c => new CachingBotDataStore(store, CachingBotDataStoreConsistencyPolicy.ETagBasedConsistency))
                        .As<IBotDataStore<BotData>>()
                        .AsSelf()
                        .InstancePerLifetimeScope();
                });
        }

        private static IContainer RegisterDependencies(ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var kbid = ConfigurationManager.AppSettings["QnaKnowledgeBaseId"];
                var key = ConfigurationManager.AppSettings["QnaSubscriptionKey"];

                var qnaAttribute = new QnAMakerAttribute(key, kbid, "I don't know what you're talking about!", 0.3);
                return new QnAMakerService(qnaAttribute);
            }).As<IQnAService>().InstancePerLifetimeScope();

            builder.Register(c =>
            {
                var luisAppId = ConfigurationManager.AppSettings["LuisAppId"];
                var luisKey = ConfigurationManager.AppSettings["LuisSubscriptionKey"];

                var luisModel = new LuisModelAttribute(luisAppId, luisKey, threshold: 0.3D);
                return new LuisService(luisModel);
            }).As<ILuisService>().InstancePerLifetimeScope();

            builder.Register(c =>
            {
                var wechatOutgoingUri = ConfigurationManager.AppSettings["WechatOutgoingURI"];
                return new MessageDispatcher(wechatOutgoingUri);
            }).As<IMessageDispatcher>().InstancePerLifetimeScope();

            return builder.Build();
        }
    }
}
