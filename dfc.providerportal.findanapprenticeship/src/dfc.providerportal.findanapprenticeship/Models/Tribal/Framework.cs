using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Tribal;
using System.Collections.Generic;

namespace Dfc.Providerportal.FindAnApprenticeship.Models.Tribal
{
    public class Framework : IFramework
    {
        public IContact Contact { get; set; }
        public int FrameworkCode { get; set; }
        public int? ProgType { get; set; }
        public int? Level { get; set; }
        public List<LocationRef> Locations { get; set; }

        public int? PathwayCode { get; set; }

        public string FrameworkInfoUrl { get; set; }

        public string MarketingInfo { get; set; }
    }
}
