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
        List<DasLocation> ApprenticeshipLocationsToLocations(int exportKey, Dictionary<string, ApprenticeshipLocation> locations);
        List<DasStandard> ApprenticeshipsToStandards(int exportKey, IEnumerable<Apprenticeship> apprenticeships,
            Dictionary<string, ApprenticeshipLocation> uniqueProviderLocations);
        List<DasFramework> ApprenticeshipsToFrameworks(int exportKey, IEnumerable<Apprenticeship> apprenticeships,
            Dictionary<string, ApprenticeshipLocation> uniqueProviderLocations);
    }
}
