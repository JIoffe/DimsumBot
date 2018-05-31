﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DimsumBot.Core.Devices;
using DimsumBot.Core.Devices.Options;
using DimsumBot.Core.DocumentDB;
using DimsumBot.Core.DocumentDB.Options;
using DimsumBot.DirectLineClient.Client;
using DimsumBot.DirectLineClient.Options;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;
using NLog.Web;
using WechatConnector.Client;
using WechatConnector.Cryptography;
using WechatConnector.Extensions;
using WechatConnector.Options;

namespace WechatConnector
{
    public class Startup
    {
        public Startup(IHostingEnvironment env, IConfiguration configuration)
        {
            HostingEnvironment = env;
            Configuration = configuration;
        }

        public IHostingEnvironment HostingEnvironment { get; }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .AddXmlSerializerFormatters();

            ConfigureOptions(services);

            services.AddSingleton<ISHA1Encryptor, SHA1Encryptor>();
            services.AddSingleton<IDocumentDBConnector, DocumentDBConnector>();

            services.AddScoped<IWechatClient, WechatClient>();
            services.AddScoped<IDirectLineConnector, DirectLineConnector>();
            services.AddScoped<IDeviceRegistrar, DocumentDBDeviceRegistrar>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IWechatClient wechatClient)
        {
            loggerFactory.AddNLog();

            var nlogConfig = "nlog.config";
            var nlogEnvSpecific = "nlog." + env.EnvironmentName + ".config";
            if (File.Exists(nlogEnvSpecific))
            {
                nlogConfig = nlogEnvSpecific;
            }

            //needed for non-NETSTANDARD platforms: configure nlog.config in your project root
            env.ConfigureNLog(nlogConfig);
            bool isRunningInAzureWebApp = !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("HOME")) && !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
            if (isRunningInAzureWebApp)
            {
                string homeDir = Environment.GetEnvironmentVariable("HOME");
                string logFilesRoot = Path.Combine(homeDir, "LogFiles", "NLog");
                NLog.LogManager.Configuration.Variables["logroot"] = logFilesRoot;
            }

            app.UseMvc();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            //Disable app insight headers for outbound wechat requests
            var modules = app.ApplicationServices.GetServices<ITelemetryModule>();
            var dependencyModule = modules.OfType<DependencyTrackingTelemetryModule>().FirstOrDefault();
            if (dependencyModule != null)
            {
                var domains = dependencyModule.ExcludeComponentCorrelationHttpHeadersOnDomains;
                domains.Add("file.api.wechat.com");
                domains.Add("file.api.weixin.qq.com");
            }


            //Setup the menu if we have to
            await wechatClient.UpdateDefaultMenu();
        }

        private void ConfigureOptions(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<WechatOptions>(Configuration.GetSection("WechatOptions"));
            services.Configure<DirectLineConnectorOptions>(Configuration.GetSection("DirectLineConnectorOptions"));
            services.Configure<DocumentDBOptions>(Configuration.GetSection("DocumentDBOptions"));
            services.Configure<DeviceRegistrationOptions>(Configuration.GetSection("DeviceRegistrationOptions"));
        }
    }
}
