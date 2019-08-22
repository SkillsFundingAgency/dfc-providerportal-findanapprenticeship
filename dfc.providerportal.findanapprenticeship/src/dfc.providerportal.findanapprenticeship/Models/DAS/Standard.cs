using Dfc.Providerportal.FindAnApprenticeship.Interfaces.DAS;
using System.Collections.Generic;

namespace Dfc.Providerportal.FindAnApprenticeship.Models.DAS
{
    public class Standard : IStandard
    {
        public int StandardCode { get; set; }
        public string MarketingInfo { get; set; }
        public string StandardInfoUrl { get; set; }
        public IContact Contact { get; set; }
        public List<LocationRef> Locations { get; set; }
      
        
        
    }
}
