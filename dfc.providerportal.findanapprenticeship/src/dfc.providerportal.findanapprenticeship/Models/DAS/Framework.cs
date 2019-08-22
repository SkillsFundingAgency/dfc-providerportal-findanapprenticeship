using Dfc.Providerportal.FindAnApprenticeship.Interfaces.DAS;
using System.Collections.Generic;

namespace Dfc.Providerportal.FindAnApprenticeship.Models.DAS
{
    public class Framework : IFramework
    {
        public int FrameworkCode { get; set; }
        public int? PathwayCode { get; set; }
        public int? ProgType { get; set; }
        public string MarketingInfo { get; set; }
        public string FrameworkInfoUrl { get; set; }
        public IContact Contact { get; set; }
        public List<LocationRef> Locations { get; set; }
  
    }
}
