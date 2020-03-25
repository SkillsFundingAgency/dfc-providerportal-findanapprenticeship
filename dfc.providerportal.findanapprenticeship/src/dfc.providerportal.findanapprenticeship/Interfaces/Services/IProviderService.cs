using System.Collections.Generic;
using System.Threading.Tasks;
using Dfc.Providerportal.FindAnApprenticeship.Models.Providers;

namespace Dfc.Providerportal.FindAnApprenticeship.Helper
{
    public interface IProviderService
    {
        IEnumerable<Provider> GetActiveProviders();
        Task<IEnumerable<Provider>> GetActiveProvidersAsync();
    }
}