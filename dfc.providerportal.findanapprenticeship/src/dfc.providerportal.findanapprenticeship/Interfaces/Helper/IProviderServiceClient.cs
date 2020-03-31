using Dfc.Providerportal.FindAnApprenticeship.Models.Providers;
using System.Collections.Generic;

namespace Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper
{
    public interface IProviderServiceClient
    {
        IEnumerable<Provider> GetAllProviders();

        IEnumerable<Provider> GetProviderByUKPRN(string UKPRN);
    }
}
