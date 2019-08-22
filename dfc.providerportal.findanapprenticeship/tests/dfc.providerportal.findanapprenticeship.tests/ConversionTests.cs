using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace dfc.providerportal.findanapprenticeship.tests
{
    public class ConversionTests
    {
        readonly Apprenticeship StandardApprenticeship = new Apprenticeship
        {
            Id = Guid.NewGuid(),
            ApprenticeshipId = 123,
            TribalProviderId = 1234,
            ApprenticeshipTitle = "Apprenticeship Title",
            ProviderId = Guid.NewGuid(),
            ProviderUKPRN = 10000000,
            ApprenticeshipType = ApprenticeshipType.StandardCode,
            StandardId = Guid.NewGuid(),
            StandardCode = 1232,
            Version = 1,
            MarketingInformation = "Standard Marketing Information",
            Url = "www.standardurl.com",
            ContactEmail = "standardapprenticeship@test.com",
            ContactWebsite = "www.standardwebsite.com",
            ApprenticeshipLocations = new List<ApprenticeshipLocation>
            {
                new ApprenticeshipLocation
                {
                    
                }
            }

            
        };
        [Fact]
        public void ApprenticeshipToDASProvider()
        {

        }
    }
}
