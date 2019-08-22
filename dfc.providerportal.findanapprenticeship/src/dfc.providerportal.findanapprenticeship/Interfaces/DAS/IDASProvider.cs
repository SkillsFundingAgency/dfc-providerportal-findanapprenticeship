using Dfc.Providerportal.FindAnApprenticeship.Models.DAS;
using System.Collections.Generic;

namespace Dfc.Providerportal.FindAnApprenticeship.Interfaces.DAS
{
    public interface IDASProvider
    {
        int? Id { get; set; }
        int? UKPRN { get; set; }
        string Email { get; set; }
        double? EmployerSatisfaction { get; set; }
        List<Framework> Frameworks { get; set; }
        double? LearnerSatisfaction { get; set; }
        List<Location> Locations { get; set; }
        string MarketingInfo { get; set; }
        string Name { get; set; }
        string TradingName { get; set; }
        bool NationalProvider { get; set; }
        string Phone { get; set; }
        List<Standard> Standards { get; set; }
        
        string Website { get; set; }
    }
}
