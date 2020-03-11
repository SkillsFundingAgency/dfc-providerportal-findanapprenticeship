﻿using System;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Models.Providers;
using Dfc.Providerportal.FindAnApprenticeship.Models.DAS;
using System.Collections.Generic;

namespace Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper
{
    [Obsolete("Please try not to use this any more, and instead create Mapper classes using Automapper or similar", false)]
    public interface IDASHelper
    {
        DASProvider CreateDASProviderFromProvider(Provider provider);
        List<Location> ApprenticeshipLocationsToLocations(IEnumerable<ApprenticeshipLocation> locations);
        List<Standard> ApprenticeshipsToStandards(IEnumerable<Apprenticeship> apprenticeships);
        List<Framework> ApprenticeshipsToFrameworks(IEnumerable<Apprenticeship> apprenticeships);
        List<Location> RegionsToLocations(string[] regionCodes);
    }
}
