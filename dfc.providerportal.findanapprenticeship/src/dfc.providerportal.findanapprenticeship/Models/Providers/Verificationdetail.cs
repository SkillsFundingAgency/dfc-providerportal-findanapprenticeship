using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Providers;

namespace Dfc.Providerportal.FindAnApprenticeship.Models.Provider
{
    public class Verificationdetail : IVerificationdetail
    {
        public string VerificationAuthority { get; set; }
        public string VerificationID { get; set; }
    }
}
