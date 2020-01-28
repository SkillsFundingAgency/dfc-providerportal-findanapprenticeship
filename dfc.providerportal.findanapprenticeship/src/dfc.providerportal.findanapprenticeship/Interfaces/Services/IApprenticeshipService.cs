using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Apprenticeships;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Models;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.DAS;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Models.Enums;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services
{
    public interface IApprenticeshipService
    {
        Task<IEnumerable<IApprenticeship>> GetApprenticeshipCollection();
        Task<IEnumerable<IApprenticeship>> GetApprenticeshipsByUkprn(int ukprn);
        IEnumerable<IDASProvider> ApprenticeshipsToDASProviders(List<Apprenticeship> apprenticeships);
    }
}
