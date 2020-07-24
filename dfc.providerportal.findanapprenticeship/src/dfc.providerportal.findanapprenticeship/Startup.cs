using System;
using Dfc.Providerportal.FindAnApprenticeship;
using Dfc.Providerportal.FindAnApprenticeship.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services;
using Dfc.Providerportal.FindAnApprenticeship.Services;
using Dfc.Providerportal.FindAnApprenticeship.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]
namespace Dfc.Providerportal.FindAnApprenticeship
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            ConfigureServices(builder.Services);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            services.AddLazyCache();

            #region Settings & Config
            
            var cosmosDbSettings = configuration.GetSection(nameof(CosmosDbSettings));
            var cosmosDbCollectionSettings = configuration.GetSection(nameof(CosmosDbCollectionSettings));
            var providerServiceSettings = configuration.GetSection(nameof(ProviderServiceSettings));
            var referenceDataServiceSettings = configuration.GetSection(nameof(ReferenceDataServiceSettings));

            services.AddSingleton<IConfiguration>(configuration);
            services.Configure<CosmosDbSettings>(cosmosDbSettings);
            services.Configure<CosmosDbCollectionSettings>(cosmosDbCollectionSettings);
            services.Configure<ProviderServiceSettings>(providerServiceSettings);
            services.Configure<ReferenceDataServiceSettings>(referenceDataServiceSettings);

            #endregion

            #region Http Clients

            services.AddHttpClient<IReferenceDataService, ReferenceDataService>(client =>
            {
                var options = referenceDataServiceSettings.Get<ReferenceDataServiceSettings>();

                client.BaseAddress = new Uri(options.ApiUrl);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", options.ApiKey);
            });

            services.AddHttpClient<IProviderService, ProviderService>(client =>
            {
                var options = providerServiceSettings.Get<ProviderServiceSettings>();

                client.BaseAddress = new Uri(options.ApiUrl);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", options.ApiKey);
            });

            #endregion

            #region Services

            services.AddSingleton<IReferenceDataServiceClient, ReferenceDataServiceClient>();
            services.AddSingleton<IProviderServiceClient, ProviderServiceClient>();
            services.AddScoped<ICosmosDbHelper, CosmosDbHelper>();
            services.AddScoped<IDASHelper, DASHelper>();
            services.AddScoped<IApprenticeshipService, ApprenticeshipService>();

            #endregion
        }
    }
}