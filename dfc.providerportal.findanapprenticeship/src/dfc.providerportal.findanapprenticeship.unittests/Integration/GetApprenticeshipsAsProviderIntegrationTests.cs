using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dfc.Providerportal.FindAnApprenticeship.Functions;
using Dfc.Providerportal.FindAnApprenticeship.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Services;
using Dfc.Providerportal.FindAnApprenticeship.Settings;
using FluentAssertions;
using LazyCache;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Dfc.ProviderPortal.FindAnApprenticeship.UnitTests.Integration
{
    public class GetApprenticeshipsAsProviderIntegrationTests
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IAppCache _appCache;
        private readonly Mock<IConfiguration> _configuration;
        private readonly Mock<ICosmosDbHelper> _cosmosDbHelper;
        private readonly IOptions<CosmosDbCollectionSettings> _cosmosSettings;

        private readonly Mock<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _referenceDataResponse;
        private readonly IReferenceDataService _referenceDataService;
        private readonly IReferenceDataServiceClient _referenceDataServiceClient;
        private readonly Mock<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _providerResponse;
        private readonly IProviderService _providerService;
        private readonly IProviderServiceClient _providerServiceClient;
        private readonly IDASHelper _DASHelper;
        private readonly IApprenticeshipService _apprenticeshipService;

        private readonly GetApprenticeshipsAsProvider _function;

        public GetApprenticeshipsAsProviderIntegrationTests()
        {
            _telemetryClient = new TelemetryClient();
            _appCache = new CachingService();
            _configuration = new Mock<IConfiguration>();
            _cosmosDbHelper = new Mock<ICosmosDbHelper>();
            _cosmosSettings = Options.Create(new CosmosDbCollectionSettings());

            _referenceDataResponse = new Mock<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>();
            _referenceDataService = new ReferenceDataService(new HttpClient(new MockHttpMessageHandler(_referenceDataResponse.Object)) { BaseAddress = new Uri("https://test.com") });
            _referenceDataServiceClient = new ReferenceDataServiceClient(_telemetryClient, new Mock<IOptions<ReferenceDataServiceSettings>>().Object, _appCache, _referenceDataService);
            _providerResponse = new Mock<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>();
            _providerService = new ProviderService(new HttpClient(new MockHttpMessageHandler(_providerResponse.Object)) { BaseAddress = new Uri("https://test.com") });
            _providerServiceClient = new ProviderServiceClient(new Mock<IOptions<ProviderServiceSettings>>().Object, _appCache, _providerService);
            
            _DASHelper = new DASHelper(_telemetryClient, _referenceDataServiceClient);
            _apprenticeshipService = new ApprenticeshipService(_telemetryClient, _cosmosDbHelper.Object, _cosmosSettings, _providerServiceClient, _DASHelper, _appCache);

            _function = new GetApprenticeshipsAsProvider(_appCache, _apprenticeshipService, _configuration.Object);
        }

        [Fact]
        public async Task Run_ReturnsExpectedResult()
        {
            _referenceDataResponse.Setup(s => s.Invoke(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Returns<HttpRequestMessage, CancellationToken>(async (r, ct) => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(await File.ReadAllTextAsync("Integration/fechoices.json"))
                });

            _providerResponse.Setup(s => s.Invoke(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Returns<HttpRequestMessage, CancellationToken>(async (r, ct) => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(await File.ReadAllTextAsync("Integration/providers.json"))
                });

            _cosmosDbHelper.Setup(s => s.GetLiveApprenticeships(It.IsAny<DocumentClient>(), It.IsAny<string>()))
                .Returns(() => JsonConvert.DeserializeObject<List<Apprenticeship>>(File.ReadAllText("Integration/apprenticeships.json")));

            var request = new Mock<HttpRequest>();

            var result = await _function.Run(request.Object, NullLogger.Instance);

            var contentResult = result.Should().BeOfType<ContentResult>().Subject;
            contentResult.Should().NotBeNull();
            contentResult.ContentType.Should().Be("application/json");
            contentResult.StatusCode.Should().Be(StatusCodes.Status200OK);

            var resultJToken = JToken.Parse(contentResult.Content);
            var expectedResultJToken = JToken.Parse(await File.ReadAllTextAsync("Integration/expectedresults.json"));

            var resultIsExpected = JToken.DeepEquals(resultJToken, expectedResultJToken);

            if (!resultIsExpected)
            {
                // Output the results so we can investigate further
                await File.WriteAllTextAsync("Integration/results.json", JsonConvert.SerializeObject(resultJToken, Formatting.Indented));
            }

            resultIsExpected.Should().BeTrue();
        }
    }
}