using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Providers;
using Dfc.Providerportal.FindAnApprenticeship.Models.Provider;
using System;
using System.ComponentModel;

namespace Dfc.Providerportal.FindAnApprenticeship.Models.Providers
{

    public class Provider : IProvider
    {

        public Provider(Providercontact[] providercontact, Provideralias[] provideraliases, Verificationdetail[] verificationdetails)
        {
            ProviderContact = providercontact;
            ProviderAliases = provideraliases;
            VerificationDetails = verificationdetails;
        }
        
        public Guid id { get; set; }
        public string UnitedKingdomProviderReferenceNumber { get; set; }
        public string ProviderName { get; set; }
        public string CourseDirectoryName { get; set; }
        public string ProviderStatus { get; set; }
        public IProvidercontact[] ProviderContact { get; set; }
        public DateTime ProviderVerificationDate { get; set; }
        public bool ProviderVerificationDateSpecified { get; set; }
        public bool ExpiryDateSpecified { get; set; }
        public object ProviderAssociations { get; set; }
        public IProvideralias[] ProviderAliases { get; set; }
        public IVerificationdetail[] VerificationDetails { get; set; }
        public Status Status { get; set; }

        // Apprenticeship related
        public int? ProviderId { get; set; }
        public int? UPIN { get; set; } // Needed to get LearnerSatisfaction & EmployerSatisfaction from FEChoices
        public string TradingName { get; set; }
        public bool NationalApprenticeshipProvider { get; set; }
        public string MarketingInformation { get; set; }
        public string Alias { get; set; }
        public ProviderType ProviderType { get; set; }


    }

    public enum Status
    {
        Registered = 0,
        Onboarded = 1,
        Unregistered = 2
    }

    public enum ProviderType
    {
        Undefined = 0,
        [Description("F.E.")]
        Fe = 1,
        [Description("Apprenticeships")]
        Apprenticeship = 2,
        [Description("Both")]
        Both = Fe | Apprenticeship
    }
}
