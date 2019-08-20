using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Settings;

namespace Dfc.Providerportal.FindAnApprenticeship.Settings
{
    public class ProviderServiceSettings : IProviderServiceSettings
    {
        public string ApiUrl { get; set; }
        public string ApiKey { get; set; }
    }
}
