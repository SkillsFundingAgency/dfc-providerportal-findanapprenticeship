using Dfc.Providerportal.FindAnApprenticeship;
using Dfc.Providerportal.FindAnApprenticeship.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services;
using Dfc.Providerportal.FindAnApprenticeship.Services;
using Dfc.Providerportal.FindAnApprenticeship.Settings;
using Dfc.ProviderPortal.Packages.AzureFunctions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Logging;

[assembly: WebJobsStartup(typeof(WebJobsExtensionStartup), "Web Jobs Extension Startup")]
namespace Dfc.Providerportal.FindAnApprenticeship
{
    public class WebJobsExtensionStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddDependencyInjection();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            builder.Services.AddLazyCache();

            #region Settings & Config
            
            var cosmosDbSettings = configuration.GetSection(nameof(CosmosDbSettings));
            var cosmosDbCollectionSettings = configuration.GetSection(nameof(CosmosDbCollectionSettings));
            var providerServiceSettings = configuration.GetSection(nameof(ProviderServiceSettings));
            var referenceDataServiceSettings = configuration.GetSection(nameof(ReferenceDataServiceSettings));

            builder.Services.AddSingleton<IConfiguration>(configuration);
            builder.Services.Configure<CosmosDbSettings>(cosmosDbSettings);
            builder.Services.Configure<CosmosDbCollectionSettings>(cosmosDbCollectionSettings);
            builder.Services.Configure<ProviderServiceSettings>(providerServiceSettings);
            builder.Services.Configure<ReferenceDataServiceSettings>(referenceDataServiceSettings);

            #endregion

            #region Http Clients

            builder.Services.AddHttpClient<IReferenceDataService, ReferenceDataService>(client =>
            {
                var options = referenceDataServiceSettings.Get<ReferenceDataServiceSettings>();

                client.BaseAddress = new Uri(options.ApiUrl);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", options.ApiKey);
            });

            builder.Services.AddHttpClient<IProviderService, ProviderService>(client =>
            {
                var options = providerServiceSettings.Get<ProviderServiceSettings>();

                client.BaseAddress = new Uri(options.ApiUrl);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", options.ApiKey);
            });

            #endregion

            #region Services

            builder.Services.AddSingleton<IReferenceDataServiceClient, ReferenceDataServiceClient>();
            builder.Services.AddSingleton<IProviderServiceClient, ProviderServiceClient>();
            builder.Services.AddScoped<ICosmosDbHelper, CosmosDbHelper>();
            builder.Services.AddScoped<IDASHelper, DASHelper>();
            builder.Services.AddScoped<IApprenticeshipService, ApprenticeshipService>();
            
            #endregion
        }
    }
}
