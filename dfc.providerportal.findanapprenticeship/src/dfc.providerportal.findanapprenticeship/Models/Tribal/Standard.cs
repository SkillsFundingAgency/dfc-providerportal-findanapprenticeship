using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Tribal;
using System.Collections.Generic;

namespace Dfc.Providerportal.FindAnApprenticeship.Models.Tribal
{
    public class Standard : IStandard
    {
        public IContact Contact { get; set; }
        public List<LocationRef> Locations { get; set; }
        public string MarketingInfo { get; set; }
        public int StandardCode { get; set; }
        public string StandardInfoUrl { get; set; }
    }
}
