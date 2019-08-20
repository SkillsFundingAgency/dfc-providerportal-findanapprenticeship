using Dfc.Providerportal.FindAnApprenticeship.Models.Providers;
using System;

namespace Dfc.Providerportal.FindAnApprenticeship.Interfaces.Providers
{
    public interface IProvider
    {
        Guid id { get; set; }
        string UnitedKingdomProviderReferenceNumber { get; set; }
        string ProviderName { get; set; }
        string CourseDirectoryName { get; set; }
        string ProviderStatus { get; set; }
        IProvidercontact[] ProviderContact { get; set; }
        DateTime ProviderVerificationDate { get; set; }
        bool ProviderVerificationDateSpecified { get; set; }
        bool ExpiryDateSpecified { get; set; }
        object ProviderAssociations { get; set; }
        IProvideralias[] ProviderAliases { get; set; }
        IVerificationdetail[] VerificationDetails { get; set; }
        Status Status { get; set; }

        // Apprenticeship related
        int? ProviderId { get; set; }
        int? UPIN { get; set; } // Needed to get LearnerSatisfaction & EmployerSatisfaction from FEChoices
        string TradingName { get; set; }
        bool NationalApprenticeshipProvider { get; set; }
        string MarketingInformation { get; set; }

        string Alias { get; set; }
    }
}
