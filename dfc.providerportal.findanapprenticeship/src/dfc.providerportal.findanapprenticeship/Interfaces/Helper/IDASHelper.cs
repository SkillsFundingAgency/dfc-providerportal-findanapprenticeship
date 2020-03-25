using System;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Models.Providers;
using Dfc.Providerportal.FindAnApprenticeship.Models.DAS;
using System.Collections.Generic;

namespace Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper
{
    [Obsolete("Please try not to use this any more, and instead create Mapper classes using Automapper or similar", false)]
    public interface IDASHelper
    {
        DasProvider CreateDasProviderFromProvider(Provider provider);
        List<DasLocation> ApprenticeshipLocationsToLocations(IEnumerable<ApprenticeshipLocation> locations);
        List<DasStandard> ApprenticeshipsToStandards(IEnumerable<Apprenticeship> apprenticeships);
        List<DasFramework> ApprenticeshipsToFrameworks(IEnumerable<Apprenticeship> apprenticeships);
        List<DasLocation> RegionsToLocations(string[] regionCodes);
    }
}
