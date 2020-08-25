using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Providers;

namespace Dfc.Providerportal.FindAnApprenticeship.Models.Providers
{
    public class Contactpersonaldetails : IContactpersonaldetails
    {
        public string[] PersonNameTitle { get; set; }
        public string[] PersonGivenName { get; set; }
        public string PersonFamilyName { get; set; }
        public object PersonNameSuffix { get; set; }
        public object PersonRequestedName { get; set; }
    }
}
