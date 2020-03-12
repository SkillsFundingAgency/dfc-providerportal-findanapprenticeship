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
    }
    
    public class ApprenticeshipServiceTests : IDisposable
    {
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
    }
}
