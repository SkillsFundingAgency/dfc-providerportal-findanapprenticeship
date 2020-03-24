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

            builder.Services.AddSingleton<IConfiguration>(configuration);
            builder.Services.Configure<CosmosDbSettings>(configuration.GetSection(nameof(CosmosDbSettings)));
            builder.Services.Configure<CosmosDbCollectionSettings>(configuration.GetSection(nameof(CosmosDbCollectionSettings)));
            builder.Services.Configure<ProviderServiceSettings>(configuration.GetSection(nameof(ProviderServiceSettings)));
            builder.Services.Configure<ReferenceDataServiceSettings>(configuration.GetSection(nameof(ReferenceDataServiceSettings)));
            builder.Services.AddSingleton<IReferenceDataServiceWrapper, ReferenceDataServiceWrapper>();
            builder.Services.AddSingleton<IProviderServiceWrapper, ProviderServiceWrapper>();
            builder.Services.AddScoped<ICosmosDbHelper, CosmosDbHelper>();
            builder.Services.AddScoped<IDASHelper, DASHelper>();
            builder.Services.AddScoped<IApprenticeshipService, ApprenticeshipService>();


        }
    }
}
