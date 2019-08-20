using Dfc.Providerportal.FindAnApprenticeship.Models.Tribal;
using System.Collections.Generic;

namespace Dfc.Providerportal.FindAnApprenticeship.Interfaces.Tribal
{
    public interface IStandard
    {
        IContact Contact { get; set; }

        List<LocationRef> Locations { get; set; }
        string MarketingInfo { get; set; }
        int StandardCode { get; set; }
        string StandardInfoUrl { get; set; }
    }
}
