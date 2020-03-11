using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Models.Enums;
using Xunit;
using NSubstitute;
using Microsoft.ApplicationInsights;

namespace Dfc.ProviderPortal.FindAnApprenticeship.UnitTests
{
    /// <summary>
    /// Unit tests for DAS Helper. Ideally this needs to go away and instead get turned into a bunch of AutoMapper classes!
    /// </summary>
    public class DasHelperTests : IDisposable
    {
        public DASHelper _dasHelper { get; set; }
        private readonly TelemetryClient _telemetryClient;
        private readonly IReferenceDataServiceWrapper _referenceDataServiceWrapper;

        public DasHelperTests()
        {
            // Arrange 
            var _telemetryClient = new TelemetryClient();
            _referenceDataServiceWrapper = Substitute.For<IReferenceDataServiceWrapper>();
            _dasHelper = new DASHelper(_telemetryClient, _referenceDataServiceWrapper);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public class ConvertToApprenticeshipDeliveryModesTests : DasHelperTests
        {
            private List<string> _validDeliveryModes = Enum.GetValues(typeof(DeliveryMode))
                    .Cast<DeliveryMode>()
                    .Select(v => v.ToDescription())
                    .ToList();

            public ConvertToApprenticeshipDeliveryModesTests()
            {
                
            }

            [Fact]
            public async Task AllMatchingModesShouldMapCorrectly()
            {
                // arrange
                var expected = _validDeliveryModes;
                var listOfDeliveryModes = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                var locationToTest = new ApprenticeshipLocation() { DeliveryModes = listOfDeliveryModes };

                // act
                var actual = _dasHelper.ConvertToApprenticeshipDeliveryModes(locationToTest);

                // assert
                Assert.Equal(expected, actual);
            }
        }
    }

    public class ApprenticeshipServiceTests : IDisposable
    {
        public IEnumerable<Apprenticeship> Apprenticeships { get; set; }

        public ApprenticeshipServiceTests()
        {
            // common test scaffolding
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public class ApprenticeshipsToDasProvidersTests : IClassFixture<ApprenticeshipServiceTests>
        {
            public ApprenticeshipsToDasProvidersTests()
            {

            }

            public async Task ShouldNotReturnEmptyFrameworksAndStandards()
            {
                // arrange


                // act

                // assert
            }
        }
    }
}
